#!/usr/bin/env bash
# Runs the test suite with coverlet and fails if line coverage is below the threshold.
# Mirrors the "Coverage" job in .github/workflows/main.yml.
#
# Usage: scripts/check-coverage.sh [--html] [--open] [threshold]
#   threshold  Minimum line coverage percentage (default: $COVERAGE_THRESHOLD or 100).
#   --html     Also generate an HTML report (ReportGenerator) in ./coverage/html/.
#   --open     Generate the HTML report and open it in the default browser.
#
# The Cobertura report is written to ./coverage/coverage.cobertura.xml.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="$REPO_ROOT/base-service-csharp.slnx"
COVERAGE_DIR="$REPO_ROOT/coverage"
HTML_DIR="$COVERAGE_DIR/html"
THRESHOLD="${COVERAGE_THRESHOLD:-100}"
HTML=false
OPEN=false

for arg in "$@"; do
    case "$arg" in
        --html) HTML=true ;;
        --open) HTML=true OPEN=true ;;
        -h | --help)
            sed -n '2,10p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        *) THRESHOLD="$arg" ;;
    esac
done

if ! [[ "$THRESHOLD" =~ ^[0-9]+$ ]] || ((THRESHOLD > 100)); then
    echo "error: threshold must be an integer between 0 and 100 (got '$THRESHOLD')" >&2
    exit 2
fi

rm -rf "$COVERAGE_DIR"

# Git Bash/MSYS: hand dotnet a Windows path, not /c/... style.
to_native() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

echo "Running tests with coverage (line threshold: ${THRESHOLD}%)..."
# -p: rather than /p: so MSYS doesn't mistake the switches for paths.
# Coverlet prints a per-module summary table and exits non-zero when below the threshold.
status=0
dotnet test "$SOLUTION" -c Release \
    -p:CollectCoverage=true \
    -p:CoverletOutputFormat=cobertura \
    "-p:CoverletOutput=$(to_native "$COVERAGE_DIR")/" \
    "-p:Threshold=$THRESHOLD" \
    -p:ThresholdType=line || status=$?

REPORT="$COVERAGE_DIR/coverage.cobertura.xml"
if [[ -f "$REPORT" ]]; then
    line_rate="$(grep -o 'line-rate="[0-9.]*"' "$REPORT" | head -n 1 | cut -d'"' -f2)"
    if [[ -n "$line_rate" ]]; then
        awk -v r="$line_rate" 'BEGIN { printf "Total line coverage: %.2f%%\n", r * 100 }'
    fi
    echo "Report: $REPORT"

    # Generated even when below the threshold: that's when you most want to see what's uncovered.
    if [[ "$HTML" == true ]]; then
        (cd "$REPO_ROOT" && dotnet tool restore >/dev/null &&
            dotnet tool run reportgenerator \
                "-reports:$(to_native "$REPORT")" \
                "-targetdir:$(to_native "$HTML_DIR")" \
                -reporttypes:Html \
                -verbosity:Warning)
        echo "HTML report: $HTML_DIR/index.html"

        if [[ "$OPEN" == true ]]; then
            if command -v cygpath >/dev/null 2>&1; then
                cmd.exe //c start "" "$(cygpath -w "$HTML_DIR/index.html")"
            elif command -v open >/dev/null 2>&1; then
                open "$HTML_DIR/index.html"
            elif command -v xdg-open >/dev/null 2>&1; then
                xdg-open "$HTML_DIR/index.html" >/dev/null 2>&1 &
            else
                echo "warning: no browser opener found; open the HTML report manually." >&2
            fi
        fi
    fi
fi

if ((status != 0)); then
    echo "Coverage check FAILED (tests failed or line coverage below ${THRESHOLD}%)." >&2
    exit "$status"
fi

echo "Coverage check passed (>= ${THRESHOLD}% line coverage)."
