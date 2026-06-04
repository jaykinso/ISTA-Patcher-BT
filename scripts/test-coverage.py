#!/usr/bin/env python3
"""Run unit tests with XPlat Code Coverage and print a summary."""

from __future__ import annotations

import argparse
import datetime
import platform
import shutil
import subprocess
import sys
import webbrowser
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parents[1]
TEST_PROJECT = ROOT_DIR / "src" / "ISTestA" / "ISTestA.csproj"
RUNSETTINGS = ROOT_DIR / "coverage.runsettings"
RESULTS_BASE = ROOT_DIR / "TestResults"


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Run unit tests with XPlat Code Coverage and print a summary.",
    )
    parser.add_argument(
        "-c", "--configuration",
        default="Debug",
        help="Build configuration. Default: Debug",
    )
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Remove TestResults and legacy coverage-report before running.",
    )
    parser.add_argument(
        "--html",
        action="store_true",
        dest="generate_html",
        help="Generate coverage-report/index.html inside this run directory.",
    )
    parser.add_argument(
        "--open",
        action="store_true",
        dest="open_html",
        help="Open this run's coverage-report/index.html after generating HTML.",
    )
    return parser


def run_command(*args: str, **kwargs: object) -> subprocess.CompletedProcess[str]:
    """Run a command and return the result."""
    return subprocess.run(args, text=True, encoding="utf-8", **kwargs)  # type: ignore[call-overload]


def find_file(directory: Path, pattern: str) -> Path | None:
    """Find the first file matching pattern in directory tree."""
    matches = list(directory.rglob(pattern))
    return matches[0] if matches else None


def print_coverage_summary(coverage_file: Path) -> None:
    """Parse cobertura XML and print a summary."""
    root = ET.parse(coverage_file).getroot()

    def pct(value: str) -> str:
        return f"{float(value) * 100:.2f}%"

    print("Coverage summary")
    print("================")
    print(
        f"Line rate:   {pct(root.attrib['line-rate'])} "
        f"({root.attrib['lines-covered']}/{root.attrib['lines-valid']})"
    )
    print(
        f"Branch rate: {pct(root.attrib['branch-rate'])} "
        f"({root.attrib['branches-covered']}/{root.attrib['branches-valid']})"
    )
    print()
    print("Packages")
    print("--------")
    for package in root.findall("./packages/package"):
        print(
            f"{package.attrib['name']}: "
            f"lines {pct(package.attrib['line-rate'])}, "
            f"branches {pct(package.attrib['branch-rate'])}"
        )


def ensure_reportgenerator() -> None:
    """Install reportgenerator global tool if not present."""
    result = run_command("reportgenerator", "--version", capture_output=True)
    if result.returncode != 0:
        print()
        print("[INFO] reportgenerator not found. Installing dotnet-reportgenerator-globaltool...")
        run_command("dotnet", "tool", "install", "-g", "dotnet-reportgenerator-globaltool", check=True)


def generate_html_report(coverage_file: Path, target_dir: Path) -> None:
    """Generate HTML coverage report using reportgenerator."""
    ensure_reportgenerator()

    print()
    print("[INFO] Generating HTML report...")
    run_command(
        "reportgenerator",
        f"-reports:{coverage_file}",
        f"-targetdir:{target_dir}",
        "-reporttypes:Html;TextSummary",
        check=True,
    )
    print(f"[INFO] HTML report: {target_dir / 'index.html'}")


def open_in_browser(path: Path) -> None:
    """Open a file in the default browser."""
    url = path.as_uri()
    print(f"[INFO] Opening {url} ...")
    webbrowser.open(url)


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    if args.open_html:
        args.generate_html = True

    if args.configuration != "Debug":
        print("[WARN] Coverage should normally run with Debug. Release may omit projects with disabled debug symbols.")

    if not TEST_PROJECT.is_file():
        print(f"[ERROR] Test project not found: {TEST_PROJECT}", file=sys.stderr)
        return 1

    if not RUNSETTINGS.is_file():
        print(f"[ERROR] Runsettings file not found: {RUNSETTINGS}", file=sys.stderr)
        return 1

    if args.clean:
        shutil.rmtree(RESULTS_BASE, ignore_errors=True)
        shutil.rmtree(ROOT_DIR / "coverage-report", ignore_errors=True)

    run_id = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    result_dir = RESULTS_BASE / run_id
    raw_result_dir = result_dir / ".dotnet-test-results"
    normalized_trx = result_dir / "unit-tests.trx"
    normalized_coverage = result_dir / "coverage.cobertura.xml"

    result_dir.mkdir(parents=True, exist_ok=True)
    raw_result_dir.mkdir(parents=True, exist_ok=True)

    print(f"[INFO] Results directory: {result_dir}")
    print("[INFO] Running tests with coverage...")

    test_result = run_command(
        "dotnet", "test",
        str(TEST_PROJECT),
        "--configuration", args.configuration,
        "--settings", str(RUNSETTINGS),
        "--collect:XPlat Code Coverage",
        "--logger", f"trx;LogFileName=unit-tests.trx",
        "--results-directory", str(raw_result_dir),
    )

    trx_file = find_file(raw_result_dir, "*.trx")
    coverage_file = find_file(raw_result_dir, "coverage.cobertura.xml")

    if trx_file is not None:
        shutil.copy2(trx_file, normalized_trx)
    else:
        normalized_trx = None  # type: ignore[assignment]

    if coverage_file is None:
        shutil.rmtree(raw_result_dir, ignore_errors=True)
        print(f"[ERROR] Coverage file was not generated under: {result_dir}", file=sys.stderr)
        return test_result.returncode

    shutil.copy2(coverage_file, normalized_coverage)
    shutil.rmtree(raw_result_dir, ignore_errors=True)

    print()
    print(f"[INFO] Test result: {normalized_trx or 'not found'}")
    print(f"[INFO] Coverage XML: {normalized_coverage}")
    print()

    print_coverage_summary(normalized_coverage)

    if args.generate_html:
        html_report_dir = result_dir / "coverage-report"
        generate_html_report(normalized_coverage, html_report_dir)
        if args.open_html:
            open_in_browser(html_report_dir / "index.html")

    return test_result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
