#!/usr/bin/env python3
"""Update release version fields after verifying CHANGELOG.md."""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT_DIR = Path(__file__).resolve().parents[1]
DEFAULT_CHANGELOG = ROOT_DIR / "CHANGELOG.md"
DEFAULT_PROJECT_GLOB = "src/**/*.csproj"
VERSION_RE = re.compile(r"^v?([0-9]+)\.([0-9]+)\.([0-9]+)(?:[-+][0-9A-Za-z.-]+)?$")
HEADING_RE = re.compile(
    r"^##\s+\[?v?(?P<version>[0-9]+(?:\.[0-9]+){2}(?:[-+][0-9A-Za-z.-]+)?)\]?"
    r"(?:\s*(?:/|-)\s*(?P<date>[0-9]{4}-[0-9]{2}-[0-9]{2})|\s*\((?P<paren_date>[0-9]{4}-[0-9]{2}-[0-9]{2})\))?"
    r"\s*$",
    re.MULTILINE,
)
NOTE_RE = re.compile(r"^\s*[-*]\s+\S", re.MULTILINE)
VERSION_TAG_RE = re.compile(r"(<(?P<tag>Version|InformationalVersion)>)([^<]*)(</(?P=tag)>)")


@dataclass(frozen=True)
class ChangelogSection:
    version: str
    date: str | None
    body: str
    line_number: int


def normalize_version(value: str) -> str:
    match = VERSION_RE.fullmatch(value.strip())
    if not match:
        raise ValueError(f"invalid release version: {value!r}; expected SemVer like 2.5.1")

    return value.strip().removeprefix("v")


def parse_changelog(path: Path) -> list[ChangelogSection]:
    text = path.read_bytes().decode("utf-8")
    matches = list(HEADING_RE.finditer(text))
    sections: list[ChangelogSection] = []

    for index, match in enumerate(matches):
        body_start = match.end()
        body_end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        line_number = text.count("\n", 0, match.start()) + 1
        sections.append(
            ChangelogSection(
                version=match.group("version"),
                date=match.group("date") or match.group("paren_date"),
                body=text[body_start:body_end],
                line_number=line_number,
            )
        )

    return sections


def require_changelog_entry(path: Path, version: str, expected_date: str | None) -> ChangelogSection:
    if not path.is_file():
        raise FileNotFoundError(f"changelog not found: {path}")

    sections = parse_changelog(path)
    section = next((item for item in sections if item.version == version), None)
    if section is None:
        raise ValueError(f"{path} has no release section for {version}")

    if expected_date is not None and section.date != expected_date:
        found = section.date or "no date"
        raise ValueError(f"{path}:{section.line_number} has date {found}; expected {expected_date}")

    if not NOTE_RE.search(section.body):
        raise ValueError(f"{path}:{section.line_number} has no bullet release notes for {version}")

    return section


def discover_project_files(pattern: str) -> list[Path]:
    return sorted(ROOT_DIR.glob(pattern))


def update_project_version(path: Path, version: str, check_only: bool) -> bool:
    text = path.read_bytes().decode("utf-8")
    seen_tags: set[str] = set()
    changed = False

    def replace(match: re.Match[str]) -> str:
        nonlocal changed
        tag = match.group("tag")
        seen_tags.add(tag)
        if match.group(3) == version:
            return match.group(0)

        changed = True
        return f"{match.group(1)}{version}{match.group(4)}"

    updated = VERSION_TAG_RE.sub(replace, text)

    missing_tags = {"Version", "InformationalVersion"} - seen_tags
    if missing_tags:
        missing = ", ".join(sorted(missing_tags))
        raise ValueError(f"{path} is missing release version tag(s): {missing}")

    if changed and not check_only:
        path.write_bytes(updated.encode("utf-8"))

    return changed


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Verify CHANGELOG.md has release notes, then update project release versions.",
    )
    parser.add_argument("version", help="Release version to set, for example 2.5.1 or v2.5.1.")
    parser.add_argument(
        "--date",
        help="Require the changelog section date to match this YYYY-MM-DD value.",
    )
    parser.add_argument(
        "--changelog",
        type=Path,
        default=DEFAULT_CHANGELOG,
        help=f"Changelog path. Default: {DEFAULT_CHANGELOG.relative_to(ROOT_DIR)}",
    )
    parser.add_argument(
        "--project",
        action="append",
        type=Path,
        dest="projects",
        help="Project file to update. Repeat to update multiple files. Default: src/**/*.csproj.",
    )
    parser.add_argument(
        "--project-glob",
        default=DEFAULT_PROJECT_GLOB,
        help=f"Project glob used when --project is omitted. Default: {DEFAULT_PROJECT_GLOB}",
    )
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="Only validate changelog and version tags; do not modify files.",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    try:
        version = normalize_version(args.version)
        changelog = args.changelog if args.changelog.is_absolute() else ROOT_DIR / args.changelog
        section = require_changelog_entry(changelog, version, args.date)

        project_files = args.projects or discover_project_files(args.project_glob)
        project_files = [path if path.is_absolute() else ROOT_DIR / path for path in project_files]
        if not project_files:
            raise ValueError(f"no project files matched {args.project_glob!r}")

        changed_files: list[Path] = []
        for project_file in project_files:
            if not project_file.is_file():
                raise FileNotFoundError(f"project file not found: {project_file}")
            if update_project_version(project_file, version, args.check_only):
                changed_files.append(project_file)

    except (OSError, ValueError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1

    changelog_path = changelog.relative_to(ROOT_DIR)
    print(f"[OK] {changelog_path}:{section.line_number} contains release notes for {version}")

    if args.check_only:
        if changed_files:
            print(f"[ERROR] {len(changed_files)} project file(s) do not already use {version}", file=sys.stderr)
            for path in changed_files:
                print(f"  - {path.relative_to(ROOT_DIR)}", file=sys.stderr)
            return 1

        print(f"[OK] {len(project_files)} project file(s) already use {version}")
        return 0

    for path in changed_files:
        print(f"[UPDATED] {path.relative_to(ROOT_DIR)}")

    if not changed_files:
        print(f"[OK] {len(project_files)} project file(s) already use {version}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
