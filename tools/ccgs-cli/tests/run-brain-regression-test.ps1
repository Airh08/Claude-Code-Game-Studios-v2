param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$brainPath = Join-Path $ProjectRoot 'project-brain'
$issuesPath = Join-Path $brainPath 'issues.yaml'

if (-not (Test-Path $ProjectRoot -PathType Container)) {
    throw "Project root does not exist: $ProjectRoot"
}

function Invoke-BrainSync {
    dotnet run --project $cliProject -- analyze $ProjectRoot --sync-brain | ConvertFrom-Json
}

function Get-IssueIds {
    $lines = Get-Content $issuesPath
    @($lines | Where-Object { $_ -match '^  - id:\s*(\S+)$' } | ForEach-Object { $Matches[1] })
}

$first = Invoke-BrainSync
$firstIds = @(Get-IssueIds)

if ($firstIds.Count -ne $first.Health.Issues.Count) {
    throw "First sync issue count mismatch. Brain has $($firstIds.Count), health report has $($first.Health.Issues.Count)."
}
Write-Host "PASS first sync issue count = $($firstIds.Count)"

$second = Invoke-BrainSync
$secondIds = @(Get-IssueIds)

if ($secondIds.Count -ne $second.Health.Issues.Count) {
    throw "Second sync issue count mismatch. Brain has $($secondIds.Count), health report has $($second.Health.Issues.Count)."
}
Write-Host "PASS second sync issue count = $($secondIds.Count)"

$firstSet = @($firstIds | Sort-Object)
$secondSet = @($secondIds | Sort-Object)
if ((Compare-Object $firstSet $secondSet)) {
    throw 'Issue IDs changed or duplicated between consecutive syncs.'
}
Write-Host 'PASS issue IDs remain stable across consecutive syncs'

$issuesText = Get-Content $issuesPath -Raw
if ($issuesText -notmatch 'history:') {
    throw 'Brain issues do not contain history data.'
}
Write-Host 'PASS issue history is persisted'

Write-Host ''
Write-Host 'Brain persistence regression test passed.'
Write-Host 'Resolution/reopen lifecycle is exercised by removing an issue from a subsequent analysis and re-running this command.'
