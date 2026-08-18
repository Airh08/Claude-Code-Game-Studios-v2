param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$fixtureRoot = Join-Path $repoRoot 'tools\test-fixtures\golden-project'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ccgs-routing-" + [Guid]::NewGuid().ToString('N'))
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

function Route-Issue([string]$code) {
    dotnet run --project $cliProject -- route $ProjectRoot --issue $code | ConvertFrom-Json
}

function Route-Task([string]$taskId) {
    dotnet run --project $cliProject -- route $ProjectRoot --task $taskId | ConvertFrom-Json
}

function Create-Task([string[]]$taskArgs) {
    dotnet run --project $cliProject -- task create $ProjectRoot @taskArgs | ConvertFrom-Json
}

function Invoke-List {
    dotnet run --project $cliProject -- task list $ProjectRoot | ConvertFrom-Json
}

try {
    # Issue-code routing: known codes must be predictable.
    $build = Route-Issue 'BUILD-001'
    Assert-Equal 'BUILD-001.PrimaryAgent' $build.PrimaryAgent 'unity-engineer'
    Assert-Equal 'BUILD-001.SupportingAgents[0]' $build.SupportingAgents[0] 'qa-engineer'
    Assert-Equal 'BUILD-001.MatchedRule' $build.MatchedRule 'issue-code:BUILD-001'
    Assert-Equal 'BUILD-001.PersistedToBrain' $build.PersistedToBrain $false

    $input = Route-Issue 'INPUT-001'
    Assert-Equal 'INPUT-001.PrimaryAgent' $input.PrimaryAgent 'unity-engineer'
    Assert-Equal 'INPUT-001.SupportingAgents[0]' $input.SupportingAgents[0] 'gameplay-programmer'

    $test = Route-Issue 'TEST-001'
    Assert-Equal 'TEST-001.PrimaryAgent' $test.PrimaryAgent 'qa-engineer'
    Assert-Equal 'TEST-001.SupportingAgents[0]' $test.SupportingAgents[0] 'unity-engineer'

    $unknownIssue = Route-Issue 'FOO-999'
    if ($null -ne $unknownIssue.PrimaryAgent) { throw "Unknown issue code should not resolve a primary agent, got '$($unknownIssue.PrimaryAgent)'." }
    Assert-Equal 'FOO-999.MatchedRule' $unknownIssue.MatchedRule 'unmatched'
    Write-Host 'PASS unknown issue codes route to no agent instead of guessing'

    # Task-type routing: representative types must be predictable.
    $uiTask = Create-Task @('--objective', 'Build main menu', '--type', 'ui')
    $uiRoute = Route-Task $uiTask.Id
    Assert-Equal 'ui.PrimaryAgent' $uiRoute.PrimaryAgent 'ui-engineer'
    Assert-Equal 'ui.SupportingAgents[0]' $uiRoute.SupportingAgents[0] 'gameplay-programmer'
    Assert-Equal 'ui.SubjectFacts.type' $uiRoute.SubjectFacts.type 'ui'
    Assert-Equal 'ui.SubjectFacts.objective' $uiRoute.SubjectFacts.objective 'Build main menu'
    Write-Host 'PASS routing artifact carries the underlying task facts (type, objective)'

    # Routing a task must persist the decision into project-brain/tasks.yaml, not just print it.
    Assert-Equal 'ui.PersistedToBrain' $uiRoute.PersistedToBrain $true
    $persistedUi = @(Invoke-List) | Where-Object { $_.Id -eq $uiTask.Id }
    Assert-Equal 'persisted.RoutedAgent' $persistedUi.RoutedAgent 'ui-engineer'
    Assert-Equal 'persisted.RoutedSupportingAgents[0]' $persistedUi.RoutedSupportingAgents[0] 'gameplay-programmer'
    Assert-Equal 'persisted.RoutingRule' $persistedUi.RoutingRule 'task-type:ui'
    if ([string]::IsNullOrWhiteSpace($persistedUi.RoutedAtUtc)) { throw 'Persisted task is missing routed_at_utc.' }
    Write-Host 'PASS routing decision is persisted into project-brain/tasks.yaml'

    # Re-routing must update the persisted decision in place, not duplicate the task.
    Start-Sleep -Milliseconds 5
    Route-Task $uiTask.Id | Out-Null
    $afterSecondRoute = @(Invoke-List)
    Assert-Equal 'afterSecondRoute.Count' $afterSecondRoute.Count 1
    Write-Host 'PASS re-routing a task updates it in place instead of duplicating it'

    $techArtTask = Create-Task @('--objective', 'Author hero shader', '--type', 'technical-art')
    $techArtRoute = Route-Task $techArtTask.Id
    Assert-Equal 'technical-art.PrimaryAgent' $techArtRoute.PrimaryAgent 'technical-artist'

    $testingTask = Create-Task @('--objective', 'Add regression coverage', '--type', 'testing')
    $testingRoute = Route-Task $testingTask.Id
    Assert-Equal 'testing.PrimaryAgent' $testingRoute.PrimaryAgent 'qa-engineer'

    $designTask = Create-Task @('--objective', 'Balance combat pacing', '--type', 'design')
    $designRoute = Route-Task $designTask.Id
    Assert-Equal 'design.PrimaryAgent' $designRoute.PrimaryAgent 'game-designer'

    $analysisTask = Create-Task @('--objective', 'Investigate frame drops', '--type', 'analysis')
    $analysisRoute = Route-Task $analysisTask.Id
    if ($null -ne $analysisRoute.PrimaryAgent) { throw "Analysis tasks should not route to a specialist agent by default, got '$($analysisRoute.PrimaryAgent)'." }
    Write-Host 'PASS analysis tasks route to no agent by default'

    # Explicit assignment must override type-based rules.
    $explicitTask = Create-Task @('--objective', 'Refactor save system', '--type', 'refactor', '--agent', 'systems-programmer')
    $explicitRoute = Route-Task $explicitTask.Id
    Assert-Equal 'explicit.PrimaryAgent' $explicitRoute.PrimaryAgent 'systems-programmer'
    Assert-Equal 'explicit.MatchedRule' $explicitRoute.MatchedRule 'explicit-assignment'

    # Ambiguous, unassigned task types must not guess an owner.
    $ambiguousTask = Create-Task @('--objective', 'Implement feature X', '--type', 'implementation')
    $ambiguousRoute = Route-Task $ambiguousTask.Id
    if ($null -ne $ambiguousRoute.PrimaryAgent) { throw "Ambiguous task types should not route to a guessed agent, got '$($ambiguousRoute.PrimaryAgent)'." }
    Assert-Equal 'ambiguous.MatchedRule' $ambiguousRoute.MatchedRule 'unmatched'
    Write-Host 'PASS ambiguous task types route to no agent instead of guessing'

    # Routing a nonexistent task must fail clearly rather than crash.
    $missingFailed = $false
    try {
        dotnet run --project $cliProject -- route $ProjectRoot --task TASK-DEADBEEF 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { $missingFailed = $true }
    } catch {
        $missingFailed = $true
    }
    if (-not $missingFailed) { throw 'Routing a nonexistent task id should fail with a nonzero exit code.' }
    $global:LASTEXITCODE = 0
    Write-Host 'PASS routing a nonexistent task id fails clearly'

    Write-Host ''
    Write-Host 'Routing rules regression test passed.'
}
finally {
    if ($ownsProject -and (Test-Path $ProjectRoot)) { Remove-Item $ProjectRoot -Recurse -Force }
}
