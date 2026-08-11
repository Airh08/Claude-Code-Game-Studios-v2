param(
    [string]$ProjectPath = "."
)

$ErrorActionPreference = "Stop"

$fullPath = (Resolve-Path $ProjectPath).Path

Write-Output "CCGS UNITY PROJECT REPORT"
Write-Output "========================="
Write-Output "Path: $fullPath"

$projectVersion = Join-Path $fullPath "ProjectSettings/ProjectVersion.txt"
if (Test-Path $projectVersion) {
    Write-Output "`n[Unity Version]"
    Get-Content $projectVersion | Where-Object { $_ -match '^m_EditorVersion:' }
} else {
    Write-Output "`n[Unity Version] UNKNOWN - ProjectSettings/ProjectVersion.txt not found"
}

$packages = Join-Path $fullPath "Packages/manifest.json"
if (Test-Path $packages) {
    Write-Output "`n[Packages]"
    Get-Content $packages
} else {
    Write-Output "`n[Packages] UNKNOWN - Packages/manifest.json not found"
}

Write-Output "`n[Assets]"
$assets = Join-Path $fullPath "Assets"
if (Test-Path $assets) {
    $scriptCount = @(Get-ChildItem $assets -Recurse -File -Filter *.cs -ErrorAction SilentlyContinue).Count
    $sceneCount = @(Get-ChildItem $assets -Recurse -File -Filter *.unity -ErrorAction SilentlyContinue).Count
    $prefabCount = @(Get-ChildItem $assets -Recurse -File -Filter *.prefab -ErrorAction SilentlyContinue).Count
    Write-Output "C# scripts: $scriptCount"
    Write-Output "Scenes: $sceneCount"
    Write-Output "Prefabs: $prefabCount"
} else {
    Write-Output "Assets directory not found"
}

Write-Output "`n[Tests]"
$testDirs = Get-ChildItem $fullPath -Recurse -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'Tests|Test' }
if ($testDirs) {
    $testDirs | Select-Object -First 20 -ExpandProperty FullName
} else {
    Write-Output "No test directories discovered"
}

Write-Output "`n[Input System hints]"
if (Test-Path $packages) {
    $manifestText = Get-Content $packages -Raw
    if ($manifestText -match 'com.unity.inputsystem') {
        Write-Output "Unity Input System package detected"
    } else {
        Write-Output "Unity Input System package not detected in manifest"
    }
}

Write-Output "`n[Warnings]"
if (-not (Test-Path $projectVersion)) { Write-Output "Missing Unity project metadata" }
if (-not (Test-Path $packages)) { Write-Output "Missing package manifest" }
if (-not (Test-Path $assets)) { Write-Output "Missing Assets directory" }

Write-Output "`nReport complete."
