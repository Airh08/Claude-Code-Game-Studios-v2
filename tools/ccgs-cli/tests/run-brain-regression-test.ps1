param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
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
    $start = ($lines | Select-String -Pattern "^  - id:\s*$([regex]::Escape($id))$").LineNumber - 1
    if ($start -lt 0) { throw "Issue not found: $id" }
    $end = $lines.Count
    for ($i = $start + 1; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^  - id:\s*') { $end = $i; break } }
    return ,$lines[$start..($end - 1)]
}

$original = if (Test-Path $issuesPath) { Get-Content $issuesPath -Raw } else { $null }
try {
    $first = Sync-Brain
    $firstIds = @(Get-Ids)
    if ($firstIds.Count -ne $first.Health.Issues.Count) { throw "First sync issue count mismatch." }
    Write-Host "PASS first sync issue count = $($firstIds.Count)"

    $second = Sync-Brain
    $secondIds = @(Get-Ids)
    if ($secondIds.Count -ne $second.Health.Issues.Count) { throw "Second sync issue count mismatch." }
    Write-Host "PASS second sync issue count = $($secondIds.Count)"

    if (Compare-Object (@($firstIds | Sort-Object)) (@($secondIds | Sort-Object))) { throw 'Issue IDs changed between consecutive syncs.' }
    Write-Host 'PASS issue IDs remain stable across consecutive syncs'

    $text = Get-Content $issuesPath -Raw
    if ($text -notmatch '(?m)^    history:$' -or $text -match 'history:\s*"\[') { throw 'History is not stored as structured YAML.' }
    Write-Host 'PASS issue history is stored as structured YAML'

    $testId = $null
    foreach ($id in $secondIds) { if ((Get-Block $id) -match '(?m)^    status:\s*open$') { $testId = $id; break } }
    if (-not $testId) { throw 'No open issue available for reopen test.' }

    $lines = Get-Content $issuesPath
    $start = ($lines | Select-String -Pattern "^  - id:\s*$([regex]::Escape($testId))$").LineNumber - 1
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^  - id:\s*') { break }
        if ($lines[$i] -match '^    status:\s*open$') { $lines[$i] = '    status: resolved'; break }
    }
    Set-Content -Path $issuesPath -Value $lines

    Sync-Brain | Out-Null
    $block = Get-Block $testId
    if ($block -notmatch '(?m)^    status:\s*open$') { throw "Issue did not reopen: $testId" }
    if ($block -notmatch '(?m)^      - status:\s*reopened$') { throw "Reopen history was not persisted: $testId" }
    Write-Host "PASS resolved -> reopened lifecycle for $testId"

    Write-Host ''
    Write-Host 'Brain persistence and reopen regression test passed.'
}
finally {
    if ($null -ne $original) { Set-Content -Path $issuesPath -Value $original -NoNewline }
}
