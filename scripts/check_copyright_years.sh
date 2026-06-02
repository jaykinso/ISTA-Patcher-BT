#!/bin/bash
# Script to check if copyright years in file headers match git history

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

base_dir="${1:-$PWD}"
base_dir=$(realpath "$base_dir")
src_dir="$base_dir/src"
src_rel="src"

if [ ! -d "$src_dir" ]; then
    echo -e "${RED}[ERROR]${NC} src directory not found: $src_dir"
    exit 1
fi

if ! git -C "$base_dir" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo -e "${RED}[ERROR]${NC} not a git repository: $base_dir"
    exit 1
fi

echo "Checking copyright years in file headers against git history..."
echo "================================================================"
echo

# Associative array compatibility for bash 3.x
_tmpdir=$(mktemp -d)
trap 'rm -rf "$_tmpdir"' EXIT
_tracked="$_tmpdir/tracked"
_untracked="$_tmpdir/untracked"
_first_years="$_tmpdir/first_years"
_last_years="$_tmpdir/last_years"
mkdir -p "$_tracked" "$_untracked" "$_first_years" "$_last_years"

_assoc_set() { mkdir -p "$(dirname "$1/$2")" && printf '%s' "$3" > "$1/$2"; }
_assoc_get() { [ -f "$1/$2" ] && cat "$1/$2" || printf ''; }
_assoc_has() { [ -f "$1/$2" ]; }
_assoc_del() { rm -f "$1/$2"; }
files=()

while IFS= read -r -d '' rel_file; do
    _assoc_set "$_tracked" "$rel_file" 1
    files+=("$rel_file")
done < <(git -C "$base_dir" ls-files -z -- "$src_rel/**/*.cs" "$src_rel/*.cs")

while IFS= read -r -d '' rel_file; do
    _assoc_set "$_untracked" "$rel_file" 1
    files+=("$rel_file")
done < <(git -C "$base_dir" ls-files --others --exclude-standard -z -- "$src_rel/**/*.cs" "$src_rel/*.cs")

current_year=""
while IFS= read -r line; do
    if [[ "$line" == __YEAR__* ]]; then
        current_year="${line#__YEAR__}"
        continue
    fi

    if [ -z "$current_year" ] || [ -z "$line" ]; then
        continue
    fi

    status="${line%%$'\t'*}"
    paths="${line#*$'\t'}"

    case "$status" in
        A*|M*)
            path="$paths"
            if [[ "$path" != *.cs ]]; then
                continue
            fi
            if ! _assoc_has "$_first_years" "$path"; then
                _assoc_set "$_first_years" "$path" "$current_year"
            fi
            _assoc_set "$_last_years" "$path" "$current_year"
            ;;
        R*)
            old_path="${paths%%$'\t'*}"
            new_path="${paths#*$'\t'}"
            if [[ "$new_path" != *.cs ]]; then
                continue
            fi
            if _assoc_has "$_first_years" "$old_path"; then
                _assoc_set "$_first_years" "$new_path" "$(_assoc_get "$_first_years" "$old_path")"
                _assoc_del "$_first_years" "$old_path"
            elif ! _assoc_has "$_first_years" "$new_path"; then
                _assoc_set "$_first_years" "$new_path" "$current_year"
            fi
            _assoc_set "$_last_years" "$new_path" "$current_year"
            _assoc_del "$_last_years" "$old_path"
            ;;
    esac
done < <(git -C "$base_dir" log --reverse --find-renames --format="__YEAR__%ad" --date=format:%Y --name-status -- "$src_rel")

for rel_file in "${files[@]}"; do
    display_path="$rel_file:1"
    if _assoc_has "$_untracked" "$rel_file"; then
        echo -e "${YELLOW}[UNTRACKED]${NC} $display_path"
        continue
    fi
    if ! _assoc_has "$_tracked" "$rel_file"; then
        continue
    fi

    first_year=$(_assoc_get "$_first_years" "$rel_file")
    last_year=$(_assoc_get "$_last_years" "$rel_file")

    # Skip if file is not in git history
    if [ -z "$first_year" ]; then
        continue
    fi

    file="$base_dir/$rel_file"

    # Extract copyright years from file header
    header_line=""
    header_line_number=""
    fallback_header_line=""
    fallback_header_line_number=""
    line_number=0
    while IFS= read -r line && [ "$line_number" -lt 5 ]; do
        line_number=$((line_number + 1))
        if [[ "$line" == *"SPDX-FileCopyrightText"* ]]; then
            header_line="$line"
            header_line_number="$line_number"
            break
        fi
        if [ -z "$fallback_header_line" ] && [[ "$line" == *"Copyright"* ]]; then
            fallback_header_line="$line"
            fallback_header_line_number="$line_number"
        fi
    done < "$file"

    if [ -z "$header_line" ]; then
        header_line="$fallback_header_line"
        header_line_number="$fallback_header_line_number"
    fi

    if [ -z "$header_line" ]; then
        echo -e "${RED}[MISSING]${NC} $display_path - No copyright header found"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Expected:    Copyright $first_year-$last_year"
        continue
    fi
    display_path="$rel_file:$header_line_number"

    # Extract year range from header (handles both "YYYY" and "YYYY-YYYY" formats)
    if [[ "$header_line" =~ Copyright[[:space:]]+([0-9]{4})-([0-9]{4}) ]]; then
        header_first_year="${BASH_REMATCH[1]}"
        header_last_year="${BASH_REMATCH[2]}"
    elif [[ "$header_line" =~ Copyright[[:space:]]+([0-9]{4}) ]]; then
        header_first_year="${BASH_REMATCH[1]}"
        header_last_year="$header_first_year"
    else
        echo -e "${RED}[ERROR]${NC} $display_path - Cannot parse copyright years from: $header_line"
        continue
    fi

    # Compare years
    status="OK"
    message=""
    # A header can predate path history after moves, splits, or extracted files. Treat
    # that as acceptable; git history is only a lower bound for the current path.
    if [ "$first_year" != "$header_first_year" ]; then
        if [ "$header_first_year" -gt "$first_year" ]; then
            status="MISMATCH"
            message="First year mismatch: git=$first_year, header=$header_first_year"
        fi
    fi

    # Check last year
    if [ "$last_year" != "$header_last_year" ]; then
        if [ -n "$message" ]; then
            message="$message; "
        fi
        if [ "$status" != "MISMATCH" ]; then
            status="MISMATCH"
        fi
        message="${message}Last year mismatch: git=$last_year, header=$header_last_year"
    fi

    if [ "$status" = "OK" ]; then
        echo -e "${GREEN}[OK]${NC} $display_path - $header_first_year-$header_last_year"
    elif [ "$status" = "WARNING" ]; then
        echo -e "${YELLOW}[WARNING]${NC} $display_path"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Header:      $header_first_year-$header_last_year"
        echo -e "  ${YELLOW}Note: $message${NC}"
    else
        echo -e "${RED}[MISMATCH]${NC} $display_path"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Header:      $header_first_year-$header_last_year"
        echo -e "  Details: $message"
    fi
done

echo
echo "================================================================"
echo "Check complete!"
