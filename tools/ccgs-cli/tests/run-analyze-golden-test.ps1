param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$expectedPath = Join-Path $repoRoot 'tools\project-scanner\tests\golden-project.expected.json'
$fixtureRoot = Join-Path $repoRoot 'tools\test-fixtures\golden-project'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = $fixtureRoot
    Write-Host "Using deterministic golden fixture: $ProjectRoot"
} else {
    Write-Host "Using explicit project override: $ProjectRoot"
}

if (-not (Test-Path $ProjectRoot -PathType Container)) {
    throw "Project root does not exist: $ProjectRoot"
}

$expected = Get-Content $expectedPath -Raw | ConvertFrom-Json
$json = dotnet run --project $cliProject -- analyze $ProjectRoot | ConvertFrom-Json

function Assert-Equal($name, $actual, $expectedValue) {
    if ($actual -ne $expectedValue) {
        throw "$name failed. Expected '$expectedValue', got '$actual'."
    }
    Write-Host "PASS $name = $actual"
}

Assert-Equal 'Health.Critical' $json.Health.Critical 0
Assert-Equal 'Health.Errors' $json.Health.Errors $expected.HealthErrorCount
Assert-Equal 'Health.Warnings' $json.Health.Warnings $expected.HealthWarningCount

$buildIssues = @($json.Health.Issues | Where-Object { $_.Code -eq 'BUILD-001' })
Assert-Equal 'BUILD-001 issue count' $buildIssues.Count 2

$paths = @($buildIssues | ForEach-Object { $_.Path })
if ($paths -notcontains 'Assets/Scenes/MainSceneV2.unity') {
    throw 'Health report did not include MainSceneV2 build issue.'
}
if ($paths -notcontains 'Assets/Scenes/MenuSceneV2.unity') {
    throw 'Health report did not include MenuSceneV2 build issue.'
}
Write-Host 'PASS Health report contains both expected BUILD-001 issues'

$inputIssues = @($json.Health.Issues | Where-Object { $_.Code -eq 'INPUT-001' })
Assert-Equal 'INPUT-001 issue count' $inputIssues.Count 1

$testIssues = @($json.Health.Issues | Where-Object { $_.Code -eq 'TEST-001' })
Assert-Equal 'TEST-001 issue count' $testIssues.Count 1

Write-Host 'CCGS analyze golden project regression test passed.'
