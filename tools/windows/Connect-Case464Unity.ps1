<#
.SYNOPSIS
Safely stage the CURRENT Unity project under the existing GitHub repository for 사건파일 No.464.
.DESCRIPTION
Dry run by default. Source project is NEVER removed or modified. No auto commit/push.
Close Unity after saving scenes before passing -Apply. Fails closed on path conflicts.
.EXAMPLE
.\Connect-Case464Unity.ps1 -UnityProjectPath "D:\MyUnityGame"
.EXAMPLE
.\Connect-Case464Unity.ps1 -UnityProjectPath "D:\MyUnityGame" -WorkspacePath "D:\Case464Repo" -Apply
#>
[CmdletBinding()]
param(
    [string] $UnityProjectPath,
    [string] $WorkspacePath = (Join-Path $env:USERPROFILE 'Documents\Case464-GitHub'),
    [switch] $Apply,
    [switch] $StageArt
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoUrl = 'https://github.com/ddoing2dang/deduction-game.git'
$repoToken = 'ddoing2dang/deduction-game'
function Stop-Setup([string] $reason) {
    Write-Host "CASE464 STOP: $reason" -ForegroundColor Red
    throw $reason
}
function Resolve-FullPath([string] $path) {
    return [System.IO.Path]::GetFullPath($path).TrimEnd([char[]]@('\','/'))
}
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Stop-Setup 'Git for Windows is missing. Install Git and repeat the dry run.'
}
if ([string]::IsNullOrWhiteSpace($UnityProjectPath)) {
    $candidates = @()
    try {
        $processes = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction Stop)
        foreach ($proc in $processes) {
            $line = [string]$proc.CommandLine
            $match = [regex]::Match($line, '(?i)-projectPath\s+(?:"([^"]+)"|(\S+))')
            if ($match.Success) {
                $p = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { $match.Groups[2].Value }
                if (Test-Path -LiteralPath $p -PathType Container) { $candidates += (Resolve-FullPath $p) }
            }
        }
    } catch {
        Write-Host 'Unity process autodetection unavailable. Supply -UnityProjectPath explicitly.' -ForegroundColor Yellow
    }
    $candidates = @($candidates | Select-Object -Unique)
    if ($candidates.Count -ne 1) {
        Stop-Setup "Could not uniquely find a running Unity project (found $($candidates.Count)). Supply -UnityProjectPath explicitly."
    }
    $UnityProjectPath = $candidates[0]
}
$source = Resolve-FullPath $UnityProjectPath
$workspace = Resolve-FullPath $WorkspacePath
if (-not (Test-Path -LiteralPath $source -PathType Container)) { Stop-Setup "Unity project not found: $source" }
foreach ($relative in @('Assets', 'Packages\manifest.json', 'ProjectSettings\ProjectVersion.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $relative))) {
        Stop-Setup "Not a valid Unity project: missing $relative in $source"
    }
}
$projectVersion = (Get-Content -LiteralPath (Join-Path $source 'ProjectSettings\ProjectVersion.txt') -TotalCount 1)
$target = Join-Path $workspace 'game\UnityProject'
$localRoot = ''
try {
    $localRoot = (& git -C $source rev-parse --show-toplevel 2>$null | Select-Object -First 1)
} catch { $localRoot = '' }
if ($localRoot) {
    $remotes = (& git -C $source remote -v 2>$null | Out-String)
    if ($remotes.Contains($repoToken)) {
        Write-Host "Already inside a Git checkout connected to $repoToken" -ForegroundColor Green
        Write-Host "Repository root: $localRoot"
        Write-Host 'No copy, commit, or push performed.'
        exit 0
    }
}
if ($source.Equals($workspace, [System.StringComparison]::OrdinalIgnoreCase) -or
    $workspace.StartsWith($source + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
    $source.StartsWith($target + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-Setup 'Workspace/source paths overlap unsafely; pick a separate empty workspace location.'
}
if (Test-Path -LiteralPath $target) {
    Stop-Setup "Target UnityProject already exists: $target. Do not overwrite. Inspect manually and resume from last success."
}
if (Test-Path -LiteralPath $workspace) {
    if (-not (Test-Path -LiteralPath (Join-Path $workspace '.git'))) {
        Stop-Setup "Workspace exists but is not a git checkout: $workspace"
    }
    $origin = (& git -C $workspace remote get-url origin 2>$null | Out-String).Trim()
    if (-not $origin.Contains($repoToken)) { Stop-Setup "Git origin does not match $repoToken. Origin: $origin" }
}
Write-Host '---- 사건파일 No.464: Unity ↔ GitHub preparation ----'
Write-Host "SOURCE:   $source"
Write-Host "UNITY:    $projectVersion"
Write-Host "WORKSPACE:$workspace"
Write-Host "TARGET:   $target"
Write-Host "STAGE ART:$StageArt"
Write-Host 'This is COPY-only. Original project and GitHub histories remain untouched.'
if (-not $Apply) {
    Write-Host 'DRY RUN ONLY. No files modified.' -ForegroundColor Yellow
    Write-Host 'Save scenes and close Unity, then rerun with -UnityProjectPath, -WorkspacePath and -Apply.'
    exit 0
}
# Copying a live Unity project may race editor writes and corrupt the staged snapshot.
try {
    $unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction Stop)
    foreach ($proc in $unityProcesses) {
        if (([string]$proc.CommandLine).IndexOf($source, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Stop-Setup 'Unity is still editing this source project. Save scenes and close its Editor before -Apply.'
        }
    }
} catch {
    if ($_.Exception.Message -like 'CASE464 STOP*' -or $_.Exception.Message -like 'Unity is still editing*') { throw }
    Stop-Setup 'Unable to verify Unity is closed. Use Task Manager to confirm and rerun.'
}
if (-not (Test-Path -LiteralPath $workspace)) {
    & git clone -- $repoUrl $workspace
    if ($LASTEXITCODE -ne 0) { Stop-Setup "Git clone failed. Inspect $workspace; do not retry blindly." }
}
if (-not (Test-Path -LiteralPath (Join-Path $workspace '.git'))) {
    Stop-Setup 'Workspace repository missing after clone.'
}
$parent = Split-Path -Parent $target
if (-not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}
New-Item -ItemType Directory -Path $target -ErrorAction Stop | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $source $folder) -Destination (Join-Path $target $folder) -Recurse -ErrorAction Stop
}
# Stage quality tools into the COPIED project, never source; do not replace existing files.
$editorDir = Join-Path $target 'Assets\Editor'
if (-not (Test-Path -LiteralPath $editorDir)) { New-Item -ItemType Directory -Path $editorDir -Force | Out-Null }
$editorScript = Join-Path $editorDir 'Case464MapWorkshop.cs'
if (-not (Test-Path -LiteralPath $editorScript)) {
    Copy-Item -LiteralPath (Join-Path $workspace 'tools\unity\Case464MapWorkshop.cs') -Destination $editorScript -ErrorAction Stop
} else {
    Write-Warning 'Editor workshop script already present in copied project; preserved without overwriting.'
}
$dataDir = Join-Path $target 'Assets\Case464\Data'
if (-not (Test-Path -LiteralPath $dataDir)) { New-Item -ItemType Directory -Path $dataDir -Force | Out-Null }
$dataTarget = Join-Path $dataDir 'room_profiles_24.v1.json'
if (-not (Test-Path -LiteralPath $dataTarget)) {
    Copy-Item -LiteralPath (Join-Path $workspace 'art\Environment\room_profiles_24.v1.json') -Destination $dataTarget -ErrorAction Stop
}
if ($StageArt) {
    $artTarget = Join-Path $target 'Assets\Case464\Art\Environment'
    if (Test-Path -LiteralPath $artTarget) {
        Stop-Setup 'Environment art target already exists, refusing overwrite. Preserve previous copy.'
    }
    $artParent = Split-Path -Parent $artTarget
    New-Item -ItemType Directory -Path $artParent -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $workspace 'art\Environment\ThirdParty') -Destination $artTarget -Recurse -ErrorAction Stop
    Write-Host 'Staged CC0 source models, NOT certified finished art. Install glTFast before importing GLB.'
}
Write-Host 'COPY SUCCESS: Unity project staged inside the GitHub working tree.' -ForegroundColor Green
Write-Host 'Original is unchanged. Nothing committed, pushed, tested or built.'
Write-Host "Open this new project in Unity Hub: $target"
Write-Host "Review git status: git -C `"$workspace`" status --short"
Write-Host 'Check Unity Console and map visuals before manually staging/committing approved work.'
