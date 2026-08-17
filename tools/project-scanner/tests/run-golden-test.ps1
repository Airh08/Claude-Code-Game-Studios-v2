param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$scannerProject = Join-Path $repoRoot 'tools\project-scanner\ProjectScanner.csproj'
$expectedPath = Join-Path $PSScriptRoot 'golden-project.expected.json'
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
$json = dotnet run --project $scannerProject -- $ProjectRoot | ConvertFrom-Json

function Assert-Equal($name, $actual, $expectedValue) {
    if ($actual -ne $expectedValue) {
        throw "$name failed. Expected '$expectedValue', got '$actual'."
    }
    Write-Host "PASS $name = $actual"
}

Assert-Equal 'UnityVersion' $json.UnityVersion $expected.UnityVersion
Assert-Equal 'IsUnityProject' $json.IsUnityProject $expected.IsUnityProject
Assert-Equal 'HasInputSystem' $json.HasInputSystem $expected.HasInputSystem
Assert-Equal 'ScriptCount' $json.Scripts.Count $expected.ScriptCount
Assert-Equal 'SceneCount' $json.Scenes.Count $expected.SceneCount
Assert-Equal 'MissingBuildSceneCount' $json.MissingBuildScenes.Count $expected.MissingBuildSceneCount
Assert-Equal 'InputCallbackWarningCount' $json.InputCallbackWarnings.Count $expected.InputCallbackWarningCount
Assert-Equal 'TestDirectoryCount' $json.TestDirectories.Count $expected.TestDirectoryCount

if ($json.MissingBuildScenes -notcontains 'Assets/Scenes/MainSceneV2.unity') {
    throw 'Expected missing build scene was not detected: Assets/Scenes/MainSceneV2.unity'
}
if ($json.MissingBuildScenes -notcontains 'Assets/Scenes/FlashbackSceneV2.unity') {
    throw 'Expected missing build scene was not detected: Assets/Scenes/FlashbackSceneV2.unity'
}
Write-Host 'PASS expected missing Build Settings scenes detected'

Write-Host 'Golden project regression test passed.'
