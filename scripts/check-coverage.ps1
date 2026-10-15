# Runs the test suite with coverlet and fails if line coverage is below the threshold.
# Mirrors the "Coverage" job in .github/workflows/main.yml.
#
# Usage: scripts/check-coverage.ps1 [-Threshold <int>] [-Html] [-Open]
#   -Threshold  Minimum line coverage percentage (default: $env:COVERAGE_THRESHOLD or 100).
#   -Html       Also generate an HTML report (ReportGenerator) in ./coverage/html/.
#   -Open       Generate the HTML report and open it in the default browser.
#
# The Cobertura report is written to ./coverage/coverage.cobertura.xml.
param(
    [ValidateRange(0, 100)]
    [int]$Threshold = $(if ($env:COVERAGE_THRESHOLD) { [int]$env:COVERAGE_THRESHOLD } else { 100 }),
    [switch]$Html,
    [switch]$Open
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'base-service-csharp.slnx'
$coverageDir = Join-Path $repoRoot 'coverage'
$htmlDir = Join-Path $coverageDir 'html'

if (Test-Path $coverageDir) {
    Remove-Item -Recurse -Force $coverageDir
}

Write-Host "Running tests with coverage (line threshold: $Threshold%)..."
# Coverlet prints a per-module summary table and exits non-zero when below the threshold.
dotnet test $solution -c Release `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    "/p:CoverletOutput=$coverageDir$([IO.Path]::DirectorySeparatorChar)" `
    "/p:Threshold=$Threshold" `
    /p:ThresholdType=line
$status = $LASTEXITCODE

$report = Join-Path $coverageDir 'coverage.cobertura.xml'
if (Test-Path $report) {
    $lineRate = ([xml](Get-Content $report)).coverage.'line-rate'
    if ($lineRate) {
        $percent = [double]::Parse($lineRate, [Globalization.CultureInfo]::InvariantCulture) * 100
        Write-Host ("Total line coverage: {0:N2}%" -f $percent)
    }
    Write-Host "Report: $report"

    # Generated even when below the threshold: that's when you most want to see what's uncovered.
    if ($Html -or $Open) {
        Push-Location $repoRoot
        try {
            dotnet tool restore | Out-Null
            dotnet tool run reportgenerator "-reports:$report" "-targetdir:$htmlDir" -reporttypes:Html -verbosity:Warning
        }
        finally {
            Pop-Location
        }
        $index = Join-Path $htmlDir 'index.html'
        Write-Host "HTML report: $index"
        if ($Open) {
            Invoke-Item $index
        }
    }
}

if ($status -ne 0) {
    Write-Error "Coverage check FAILED (tests failed or line coverage below $Threshold%)." -ErrorAction Continue
    exit $status
}

Write-Host "Coverage check passed (>= $Threshold% line coverage)."
