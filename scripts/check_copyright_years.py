#!/usr/bin/env python3
"""Check if copyright years in C# file headers match git history."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parents[1]
SRC_DIR = ROOT_DIR / "src"
SRC_REL = "src"

COPYRIGHT_RE = re.compile(r"Copyright\s+(\d{4})(?:-(\d{4}))?")
SPDX_RE = re.compile(r"SPDX-FileCopyrightText")

# ANSI color codes
GREEN = "\033[0;32m"
RED = "\033[0;31m"
YELLOW = "\033[1;33m"
NC = "\033[0m"


def git_output(*args: str, cwd: Path | None = None) -> str:
    """Run a git command and return stdout, raise on failure."""
    result = subprocess.run(
        ["git", *args],
        cwd=cwd or ROOT_DIR,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    result.check_returncode()
    return result.stdout


def get_git_tracked_files() -> list[str]:
    """Get all tracked .cs files under src/."""
    output = git_output("ls-files", "-z", "--", f"{SRC_REL}/**/*.cs", f"{SRC_REL}/*.cs")
    return [f for f in output.split("\0") if f]


def get_git_untracked_files() -> list[str]:
    """Get all untracked .cs files under src/."""
    output = git_output("ls-files", "--others", "--exclude-standard", "-z", "--", f"{SRC_REL}/**/*.cs", f"{SRC_REL}/*.cs")
    return [f for f in output.split("\0") if f]


def get_git_year_ranges() -> tuple[dict[str, str], dict[str, str]]:
    """
    Parse git log to get first and last modification year for each .cs file.
    Returns (first_years: dict[path, year], last_years: dict[path, year]).
    """
    first_years: dict[str, str] = {}
    last_years: dict[str, str] = {}

    output = git_output(
        "log", "--reverse", "--find-renames",
        "--format=__YEAR__%ad",
        "--date=format:%Y",
        "--name-status",
        "--", SRC_REL,
    )

    current_year: str | None = None
    for line in output.splitlines():
        if line.startswith("__YEAR__"):
            current_year = line.removeprefix("__YEAR__")
            continue

        if not current_year or not line:
            continue

        # Lines are: <status>\t<path>  or  R<score>\t<old>\t<new>
        parts = line.split("\t")
        status = parts[0]

        if status.startswith(("A", "M")):
            path = parts[1]
            if not path.endswith(".cs"):
                continue
            if path not in first_years:
                first_years[path] = current_year
            last_years[path] = current_year

        elif status.startswith("R"):
            if len(parts) < 3:
                continue
            old_path, new_path = parts[1], parts[2]
            if not new_path.endswith(".cs"):
                continue
            if old_path in first_years:
                first_years[new_path] = first_years.pop(old_path)
            elif new_path not in first_years:
                first_years[new_path] = current_year
            last_years[new_path] = current_year
            last_years.pop(old_path, None)

    return first_years, last_years


def parse_copyright_header(file_path: Path) -> tuple[str | None, str | None, int]:
    """
    Parse the copyright years from a file header.
    Returns (first_year, last_year, line_number) or (None, None, 0).
    """
    try:
        content = file_path.read_text(encoding="utf-8")
    except OSError:
        return None, None, 0

    lines = content.splitlines()
    for i, line in enumerate(lines[:5], start=1):
        if SPDX_RE.search(line):
            match = COPYRIGHT_RE.search(line)
            if match:
                first = match.group(1)
                last = match.group(2) or first
                return first, last, i
            return None, None, 0

    # Fallback: look for any Copyright line in the first 5 lines
    for i, line in enumerate(lines[:5], start=1):
        if "Copyright" in line:
            match = COPYRIGHT_RE.search(line)
            if match:
                first = match.group(1)
                last = match.group(2) or first
                return first, last, i

    return None, None, 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Check if copyright years in C# file headers match git history.",
    )
    parser.add_argument(
        "base_dir",
        nargs="?",
        type=Path,
        default=ROOT_DIR,
        help=f"Base directory of the repository. Default: {ROOT_DIR}",
    )
    args = parser.parse_args(argv)

    base_dir = args.base_dir.resolve()
    src_dir = base_dir / "src"

    if not src_dir.is_dir():
        print(f"{RED}[ERROR]{NC} src directory not found: {src_dir}", file=sys.stderr)
        return 1

    if not (base_dir / ".git").exists():
        print(f"{RED}[ERROR]{NC} not a git repository: {base_dir}", file=sys.stderr)
        return 1

    print("Checking copyright years in file headers against git history...")
    print("=" * 64)
    print()

    try:
        tracked = set(get_git_tracked_files())
        untracked = set(get_git_untracked_files())
        first_years, last_years = get_git_year_ranges()
    except subprocess.CalledProcessError as e:
        print(f"{RED}[ERROR]{NC} git command failed: {e}", file=sys.stderr)
        return 1

    all_files = sorted(tracked | untracked)

    for rel_file in all_files:
        display = f"{rel_file}:1"

        if rel_file in untracked:
            print(f"{YELLOW}[UNTRACKED]{NC} {display}")
            continue

        if rel_file not in tracked:
            continue

        first_year = first_years.get(rel_file)
        last_year = last_years.get(rel_file)
        if not first_year:
            continue

        file_path = base_dir / rel_file
        header_first, header_last, line_num = parse_copyright_header(file_path)

        if header_first is None:
            print(f"{RED}[MISSING]{NC} {display} - No copyright header found")
            print(f"  Git history: {first_year}-{last_year}")
            print(f"  Expected:    Copyright {first_year}-{last_year}")
            continue

        display = f"{rel_file}:{line_num}"

        # Compare
        status = "OK"
        messages: list[str] = []

        # Header can predate path history after moves/splits/extractions.
        # Git history is only a lower bound for the current path.
        if first_year != header_first:
            if int(header_first) > int(first_year):
                status = "MISMATCH"
                messages.append(f"First year mismatch: git={first_year}, header={header_first}")

        if last_year != header_last:
            if status != "MISMATCH":
                status = "MISMATCH"
            messages.append(f"Last year mismatch: git={last_year}, header={header_last}")

        if status == "OK":
            print(f"{GREEN}[OK]{NC} {display} - {header_first}-{header_last}")
        else:
            print(f"{RED}[MISMATCH]{NC} {display}")
            print(f"  Git history: {first_year}-{last_year}")
            print(f"  Header:      {header_first}-{header_last}")
            print(f"  Details: {'; '.join(messages)}")

    print()
    print("=" * 64)
    print("Copyright year check complete.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
