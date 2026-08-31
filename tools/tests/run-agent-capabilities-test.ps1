# Deterministic checks for M4.3: agent capability metadata must stay in
# sync with the actual agent definitions and with itself. This does not
# validate an agent's real-world behavior — it validates that the data
# TaskRouter reads is internally consistent and matches what actually
# exists on disk, so a rename/typo/drift is caught immediately instead of
# surfacing later as a silent misroute.

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cliProject = Join-Path $repoRoot 'tools\ccgs-cli\Ccgs.Cli.csproj'
$agentsDir = Join-Path $repoRoot '.claude\agents'
$capabilitiesFile = Join-Path $agentsDir 'capabilities.json'

if (-not (Test-Path $capabilitiesFile -PathType Leaf)) { throw "Missing capability registry: $capabilitiesFile" }

function Assert-Equal($name, $actual, $expectedValue) {
    if ($actual -ne $expectedValue) {
        throw "$name failed. Expected '$expectedValue', got '$actual'."
    }
    Write-Host "PASS $name = $actual"
}

$registry = Get-Content $capabilitiesFile -Raw | ConvertFrom-Json
$registryNames = @($registry.agents | ForEach-Object { $_.name }) | Sort-Object
$agentFileNames = @(Get-ChildItem -Path $agentsDir -Filter '*.md' | ForEach-Object { $_.BaseName }) | Sort-Object

$missingFromDisk = @($registryNames | Where-Object { $agentFileNames -notcontains $_ })
if ($missingFromDisk.Count -gt 0) { throw "capabilities.json references agent(s) with no .claude/agents/<name>.md file: $($missingFromDisk -join ', ')" }

$missingFromRegistry = @($agentFileNames | Where-Object { $registryNames -notcontains $_ })
if ($missingFromRegistry.Count -gt 0) { throw "Agent definition(s) missing from capabilities.json: $($missingFromRegistry -join ', ')" }

Write-Host "PASS every agent definition has exactly one capabilities.json entry ($($agentFileNames.Count) agents)"

foreach ($rule in $registry.issue_code_rules) {
    if ($null -ne $rule.primary -and $registryNames -notcontains $rule.primary) {
        throw "issue_code_rules['$($rule.code)'].primary references unknown agent: $($rule.primary)"
    }
    foreach ($supporting in $rule.supporting) {
        if ($registryNames -notcontains $supporting) { throw "issue_code_rules['$($rule.code)'].supporting references unknown agent: $supporting" }
    }
}
foreach ($rule in $registry.task_type_rules) {
    if ($null -ne $rule.primary -and $registryNames -notcontains $rule.primary) {
        throw "task_type_rules['$($rule.type)'].primary references unknown agent: $($rule.primary)"
    }
    foreach ($supporting in $rule.supporting) {
        if ($registryNames -notcontains $supporting) { throw "task_type_rules['$($rule.type)'].supporting references unknown agent: $supporting" }
    }
}
Write-Host 'PASS every routing rule references an agent that exists in the registry'

$allAgents = dotnet run --project $cliProject -- agents list | ConvertFrom-Json
Assert-Equal 'agents list count' $allAgents.Count $agentFileNames.Count

$specialists = dotnet run --project $cliProject -- agents list --role specialist | ConvertFrom-Json
$orchestration = dotnet run --project $cliProject -- agents list --role orchestration | ConvertFrom-Json
Assert-Equal 'specialists + orchestration == total' ($specialists.Count + $orchestration.Count) $allAgents.Count

if ($orchestration.Name -notcontains 'game-director' -or $orchestration.Name -notcontains 'task-router') {
    throw "Expected game-director and task-router in the orchestration role; got: $($orchestration.Name -join ', ')"
}
Write-Host 'PASS --role filtering matches the expected orchestration/specialist split'

$unityEngineer = $allAgents | Where-Object { $_.Name -eq 'unity-engineer' }
if ($unityEngineer.RequiredValidators -notcontains 'build') { throw 'unity-engineer capability entry is missing its expected required_validators.' }
Write-Host 'PASS a representative capability entry (unity-engineer) carries its declared fields through the CLI'

# M4.4: every required_validators value must be one of the evidence.type values
# documented in the Output Contract (.claude/rules/agents.md), or the two would
# silently drift apart.
$allowedEvidenceTypes = @('test', 'build', 'manual-verification', 'scan')
foreach ($agent in $registry.agents) {
    foreach ($validator in $agent.required_validators) {
        if ($allowedEvidenceTypes -notcontains $validator) {
            throw "Agent '$($agent.name)' declares required_validators entry '$validator', which is not one of the evidence.type values in .claude/rules/agents.md ($($allowedEvidenceTypes -join ', '))."
        }
    }
}
Write-Host 'PASS every required_validators value matches a documented evidence.type'

Write-Host ''
Write-Host 'Agent capability registry regression test passed.'
