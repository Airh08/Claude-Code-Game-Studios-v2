param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$fixtureRoot = Join-Path $repoRoot 'tools\test-fixtures\golden-project'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ccgs-brain-regression-" + [Guid]::NewGuid().ToString('N'))
    Copy-Item $fixtureRoot $ProjectRoot -Recurse
    $ownsProject = $true
    Write-Host "Using isolated golden fixture copy: $ProjectRoot"
} else {
    $ownsProject = $false
    Write-Host "Using explicit project override: $ProjectRoot"
}

$brainPath = Join-Path $ProjectRoot 'project-brain'
$issuesPath = Join-Path $brainPath 'issues.yaml'

if (-not (Test-Path $ProjectRoot -PathType Container)) { throw "Project root does not exist: $ProjectRoot" }

function Sync-Brain {
    dotnet run --project $cliProject -- analyze $ProjectRoot --sync-brain | ConvertFrom-Json
}

function Get-Ids {
    @(Get-Content $issuesPath | Where-Object { $_ -match '^  - id:\s*(\S+)$' } | ForEach-Object { $Matches[1] })
}

function Get-Block([string]$id) {
    $lines = Get-Content $issuesPath
    $match = $lines | Select-String -Pattern "^  - id:\s*$([regex]::Escape($id))$" | Select-Object -First 1
    if ($null -eq $match) { throw "Issue not found: $id" }
    $start = $match.LineNumber - 1
    $end = $lines.Count
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^  - id:\s*') { $end = $i; break }
    }
    return ,$lines[$start..($end - 1)]
}

try {
    $first = Sync-Brain
    $firstIds = @(Get-Ids)
    if ($firstIds.Count -ne 4 -or $first.Health.Issues.Count -ne 4) { throw "First sync expected 4 issues; got brain=$($firstIds.Count), health=$($first.Health.Issues.Count)." }
    Write-Host "PASS first sync issue count = $($firstIds.Count)"

    $second = Sync-Brain
    $secondIds = @(Get-Ids)
    if ($secondIds.Count -ne 4 -or $second.Health.Issues.Count -ne 4) { throw "Second sync expected 4 issues; got brain=$($secondIds.Count), health=$($second.Health.Issues.Count)." }
    Write-Host "PASS second sync issue count = $($secondIds.Count)"

    if (Compare-Object (@($firstIds | Sort-Object)) (@($secondIds | Sort-Object))) { throw 'Issue IDs changed between consecutive syncs.' }
    Write-Host 'PASS issue IDs remain stable across consecutive syncs'

    $text = Get-Content $issuesPath -Raw
    if ($text -notmatch '(?m)^    history:$' -or $text -match 'history:\s*"\[') { throw 'History is not stored as structured YAML.' }
    Write-Host 'PASS issue history is stored as structured YAML'

    $testId = $secondIds[0]
    $lines = Get-Content $issuesPath
    $startMatch = $lines | Select-String -Pattern "^  - id:\s*$([regex]::Escape($testId))$" | Select-Object -First 1
    $start = $startMatch.LineNumber - 1
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^  - id:\s*') { break }
        if ($lines[$i] -match '^    status:\s*open$') { $lines[$i] = '    status: resolved'; break }
    }
    Set-Content -Path $issuesPath -Value $lines

    Sync-Brain | Out-Null
    $block = (Get-Block $testId) -join "`n"
    if ($block -notmatch '(?m)^    status:\s*open$') { throw "Issue did not reopen: $testId" }
    if ($block -notmatch '(?m)^      - status: reopened$') { throw "Reopen history was not persisted: $testId" }
    Write-Host "PASS resolved -> reopened lifecycle for $testId"

    Write-Host ''
    Write-Host 'Brain persistence and reopen regression test passed.'
}
finally {
    if ($ownsProject -and (Test-Path $ProjectRoot)) { Remove-Item $ProjectRoot -Recurse -Force }
}
