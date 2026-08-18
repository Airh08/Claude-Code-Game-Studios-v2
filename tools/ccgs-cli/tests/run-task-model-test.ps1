param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$fixtureRoot = Join-Path $repoRoot 'tools\test-fixtures\golden-project'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ccgs-task-model-" + [Guid]::NewGuid().ToString('N'))
    Copy-Item $fixtureRoot $ProjectRoot -Recurse
    $ownsProject = $true
    Write-Host "Using isolated golden fixture copy: $ProjectRoot"
} else {
    $ownsProject = $false
    Write-Host "Using explicit project override: $ProjectRoot"
}

if (-not (Test-Path $ProjectRoot -PathType Container)) { throw "Project root does not exist: $ProjectRoot" }

function Assert-Equal($name, $actual, $expectedValue) {
    if ($actual -ne $expectedValue) {
        throw "$name failed. Expected '$expectedValue', got '$actual'."
    }
    Write-Host "PASS $name = $actual"
}

function Invoke-Create([string[]]$taskArgs) {
    dotnet run --project $cliProject -- task create $ProjectRoot @taskArgs | ConvertFrom-Json
}

function Invoke-List {
    dotnet run --project $cliProject -- task list $ProjectRoot | ConvertFrom-Json
}

try {
    $first = Invoke-Create @(
        '--objective', 'Fix missing Build Settings scene reference',
        '--type', 'implementation',
        '--priority', 'high',
        '--agent', 'unity-engineer',
        '--path', 'Assets/Scenes/MainSceneV2.unity',
        '--path', 'Assets/Scenes/MenuSceneV2.unity',
        '--constraint', 'Do not touch unrelated scenes',
        '--validation', 'Scanner reports zero missing build scenes'
    )

    if ($first.Id -notmatch '^TASK-[0-9A-F]{8}$') { throw "First task id does not match expected format: $($first.Id)" }
    Write-Host "PASS first task id format = $($first.Id)"

    Assert-Equal 'first.Objective' $first.Objective 'Fix missing Build Settings scene reference'
    Assert-Equal 'first.Type' $first.Type 'implementation'
    Assert-Equal 'first.Priority' $first.Priority 'high'
    Assert-Equal 'first.Status' $first.Status 'open'
    Assert-Equal 'first.AssignedAgent' $first.AssignedAgent 'unity-engineer'
    Assert-Equal 'first.AffectedPaths.Count' $first.AffectedPaths.Count 2
    Assert-Equal 'first.Constraints.Count' $first.Constraints.Count 1
    Assert-Equal 'first.Dependencies.Count' $first.Dependencies.Count 0
    Assert-Equal 'first.ValidationRequirements.Count' $first.ValidationRequirements.Count 1

    $second = Invoke-Create @(
        '--objective', 'Review Build Settings fix',
        '--type', 'testing',
        '--agent', 'qa-engineer',
        '--depends-on', $first.Id
    )

    Assert-Equal 'second.Priority' $second.Priority 'medium'
    Assert-Equal 'second.AffectedPaths.Count' $second.AffectedPaths.Count 0
    Assert-Equal 'second.Dependencies.Count' $second.Dependencies.Count 1
    Assert-Equal 'second.Dependencies[0]' $second.Dependencies[0] $first.Id

    if ($first.Id -eq $second.Id) { throw 'Task ids are not unique.' }
    Write-Host 'PASS task ids are unique'

    $listed = @(Invoke-List)
    Assert-Equal 'listed.Count' $listed.Count 2

    $listedFirst = $listed | Where-Object { $_.Id -eq $first.Id }
    Assert-Equal 'listedFirst.AffectedPaths[0]' $listedFirst.AffectedPaths[0] 'Assets/Scenes/MainSceneV2.unity'
    Assert-Equal 'listedFirst.AffectedPaths[1]' $listedFirst.AffectedPaths[1] 'Assets/Scenes/MenuSceneV2.unity'
    Assert-Equal 'listedFirst.Constraints[0]' $listedFirst.Constraints[0] 'Do not touch unrelated scenes'
    Assert-Equal 'listedFirst.ValidationRequirements[0]' $listedFirst.ValidationRequirements[0] 'Scanner reports zero missing build scenes'
    Write-Host 'PASS persisted list fields round-trip through tasks.yaml'

    $listedAgain = @(Invoke-List)
    $a = ($listed | ConvertTo-Json -Depth 10)
    $b = ($listedAgain | ConvertTo-Json -Depth 10)
    if ($a -ne $b) { throw 'Repeated list calls produced different output; task persistence is not deterministic.' }
    Write-Host 'PASS repeated list calls are deterministic'

    $tasksPath = Join-Path $ProjectRoot 'project-brain\tasks.yaml'
    $raw = Get-Content $tasksPath -Raw
    if ($raw -match "`r`n") { throw 'tasks.yaml contains CRLF line endings; expected deterministic LF output.' }
    Write-Host 'PASS tasks.yaml uses LF line endings'

    Write-Host ''
    Write-Host 'Task model regression test passed.'
}
finally {
    if ($ownsProject -and (Test-Path $ProjectRoot)) { Remove-Item $ProjectRoot -Recurse -Force }
}
