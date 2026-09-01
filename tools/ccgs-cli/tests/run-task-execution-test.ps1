param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$fixtureRoot = Join-Path $repoRoot 'tools\test-fixtures\golden-project'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ccgs-task-execution-" + [Guid]::NewGuid().ToString('N'))
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

function Create-Task([string[]]$taskArgs) {
    dotnet run --project $cliProject -- task create $ProjectRoot @taskArgs | ConvertFrom-Json
}

function Route-Task([string]$taskId) {
    dotnet run --project $cliProject -- route $ProjectRoot --task $taskId | Out-Null
}

function Expect-Failure([string]$description, [ScriptBlock]$action) {
    $failed = $false
    $global:LASTEXITCODE = 0
    try {
        & $action 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { $failed = $true }
    } catch {
        $failed = $true
    }
    $global:LASTEXITCODE = 0
    if (-not $failed) { throw "Expected failure but succeeded: $description" }
    Write-Host "PASS $description"
}

try {
    # Cannot start execution before the task is routed.
    $unrouted = Create-Task @('--objective', 'Unrouted task', '--type', 'implementation')
    Expect-Failure 'starting an unrouted task fails' { dotnet run --project $cliProject -- task start $ProjectRoot $unrouted.Id }

    # Happy path: route -> start -> complete requires passing evidence.
    $task = Create-Task @('--objective', 'Fix missing scene', '--type', 'implementation', '--agent', 'unity-engineer')
    Route-Task $task.Id

    $started = dotnet run --project $cliProject -- task start $ProjectRoot $task.Id | ConvertFrom-Json
    Assert-Equal 'started.ExecutionStatus' $started.ExecutionStatus 'executing'
    if ([string]::IsNullOrWhiteSpace($started.ExecutionStartedAtUtc)) { throw 'ExecutionStartedAtUtc was not set.' }

    Expect-Failure 'completing without evidence fails' { dotnet run --project $cliProject -- task complete $ProjectRoot $task.Id --summary 'done' }
    Expect-Failure 'completing with only failing evidence fails' { dotnet run --project $cliProject -- task complete $ProjectRoot $task.Id --summary 'done' --evidence 'build:fail' }

    $completed = dotnet run --project $cliProject -- task complete $ProjectRoot $task.Id --summary 'Fixed the missing scene reference' --evidence 'scan:pass' --evidence 'build:pass' | ConvertFrom-Json
    Assert-Equal 'completed.ExecutionStatus' $completed.ExecutionStatus 'completed'
    Assert-Equal 'completed.ExecutionSummary' $completed.ExecutionSummary 'Fixed the missing scene reference'
    Assert-Equal 'completed.ExecutionEvidence.Count' $completed.ExecutionEvidence.Count 2

    # A completed task cannot be completed again, started again, or blocked.
    Expect-Failure 're-completing a completed task fails' { dotnet run --project $cliProject -- task complete $ProjectRoot $task.Id --summary 'again' --evidence 'build:pass' }
    Expect-Failure 're-starting a completed task fails' { dotnet run --project $cliProject -- task start $ProjectRoot $task.Id }
    Expect-Failure 'blocking a completed task fails' { dotnet run --project $cliProject -- task block $ProjectRoot $task.Id --summary 'nope' }

    # Persisted state survives a fresh read (task list), not just the transition's own output.
    $persisted = @(dotnet run --project $cliProject -- task list $ProjectRoot | ConvertFrom-Json) | Where-Object { $_.Id -eq $task.Id }
    Assert-Equal 'persisted.ExecutionStatus' $persisted.ExecutionStatus 'completed'
    Assert-Equal 'persisted.ExecutionEvidence[1]' $persisted.ExecutionEvidence[1] 'build:pass'
    Write-Host 'PASS execution state round-trips through project-brain/tasks.yaml'

    # Fail -> restart cycle: a failed task can be retried, unlike a completed one.
    $task2 = Create-Task @('--objective', 'Second task', '--type', 'ui')
    Route-Task $task2.Id
    dotnet run --project $cliProject -- task start $ProjectRoot $task2.Id | Out-Null
    $failed = dotnet run --project $cliProject -- task fail $ProjectRoot $task2.Id --summary 'Compile error' --evidence 'build:fail' | ConvertFrom-Json
    Assert-Equal 'failed.ExecutionStatus' $failed.ExecutionStatus 'failed'

    $restarted = dotnet run --project $cliProject -- task start $ProjectRoot $task2.Id | ConvertFrom-Json
    Assert-Equal 'restarted.ExecutionStatus' $restarted.ExecutionStatus 'executing'
    if ($restarted.ExecutionSummary) { throw 'Restarting should clear the previous failure summary.' }
    Write-Host 'PASS a failed task can be restarted, clearing the prior failure'

    # Block can happen mid-execution (e.g. missing required input) without prior failure.
    $task3 = Create-Task @('--objective', 'Third task', '--type', 'testing')
    Route-Task $task3.Id
    dotnet run --project $cliProject -- task start $ProjectRoot $task3.Id | Out-Null
    $blocked = dotnet run --project $cliProject -- task block $ProjectRoot $task3.Id --summary 'Missing test fixture' | ConvertFrom-Json
    Assert-Equal 'blocked.ExecutionStatus' $blocked.ExecutionStatus 'blocked'
    Assert-Equal 'blocked.ExecutionSummary' $blocked.ExecutionSummary 'Missing test fixture'

    Write-Host ''
    Write-Host 'Task execution lifecycle regression test passed.'
}
finally {
    if ($ownsProject -and (Test-Path $ProjectRoot)) { Remove-Item $ProjectRoot -Recurse -Force }
}
