# Deterministic structural check for M4.1/M4.2: every agent definition must
# document that it follows the shared Input/Output Contract, and the shared
# contract itself must define both halves. This does not (and cannot) verify
# an LLM agent's behavior — it verifies the documentation exists and stays
# wired together, so contract drift is caught without relying on a person
# noticing a missing section.

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$agentsDir = Join-Path $repoRoot '.claude\agents'
$contractFile = Join-Path $repoRoot '.claude\rules\agents.md'

if (-not (Test-Path $contractFile -PathType Leaf)) { throw "Missing shared agent contract: $contractFile" }

$contractText = Get-Content $contractFile -Raw
foreach ($heading in @('## Input Contract', '## Output Contract', '## Escalation')) {
    if ($contractText -notmatch [regex]::Escape($heading)) { throw "$contractFile is missing required heading: $heading" }
}
Write-Host "PASS $contractFile defines Input Contract, Output Contract, and Escalation"

$agentFiles = @(Get-ChildItem -Path $agentsDir -Filter '*.md' | Sort-Object Name)
if ($agentFiles.Count -eq 0) { throw "No agent definitions found under $agentsDir" }

foreach ($file in $agentFiles) {
    $text = Get-Content $file.FullName -Raw

    if ($text -notmatch '(?m)^name:\s*(\S+)') { throw "$($file.Name) is missing a 'name:' frontmatter field." }
    $name = $Matches[1]

    if ($text -notmatch '(?m)^## Contract\b') { throw "$($file.Name) ($name) is missing a '## Contract' section." }
    if ($text -notmatch '\.claude/rules/agents\.md') { throw "$($file.Name) ($name) does not reference the shared contract in .claude/rules/agents.md." }

    Write-Host "PASS $($file.Name) ($name) documents its contract"
}

Write-Host ''
Write-Host "Agent contract structure regression test passed ($($agentFiles.Count) agents)."
