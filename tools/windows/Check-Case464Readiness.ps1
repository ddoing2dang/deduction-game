<#
Read-only local readiness report for 사건파일 No.464. No editor calls, files modified, tests or builds.
Usage:
  .\Check-Case464Readiness.ps1 -WorkspacePath "D:\Case464-GitHub"
#>
[CmdletBinding()]
param(
    [string]$WorkspacePath = (Join-Path $env:USERPROFILE 'Documents\Case464-GitHub'),
    [string]$UnityProjectPath = ''
)
$ErrorActionPreference = 'Continue'
$repoPath = [System.IO.Path]::GetFullPath($WorkspacePath)
if ([string]::IsNullOrWhiteSpace($UnityProjectPath)) {
    $UnityProjectPath = Join-Path $repoPath 'game\UnityProject'
}
$project = [System.IO.Path]::GetFullPath($UnityProjectPath)
Write-Host '=== CASE464 / READ-ONLY CONNECTION REPORT ==='
Write-Host "Repository: $repoPath"
Write-Host "Unity project: $project"
function Report([string]$name, [string]$value) { Write-Host ("{0,-27}: {1}" -f $name, $value) }
if (Test-Path (Join-Path $repoPath '.git')) {
    $remote = (& git -C $repoPath remote get-url origin 2>$null | Out-String).Trim()
    $head = (& git -C $repoPath rev-parse --short HEAD 2>$null | Out-String).Trim()
    Report 'GitHub origin' $remote
    Report 'Git HEAD' $head
    Report 'Remote correct' $(if ($remote.Contains('ddoing2dang/deduction-game')) { 'YES' } else { 'REVIEW' })
    Report 'Local changes' ((& git -C $repoPath status --short 2>$null | Measure-Object -Line).Lines.ToString())
} else { Report 'Repository' 'NOT CLONED / NOT CONNECTED' }
if (-not (Test-Path (Join-Path $project 'Assets'))) {
    Report 'Unity project' 'MISSING - no project files inspected'
    exit 0
}
$versionFile = Join-Path $project 'ProjectSettings\ProjectVersion.txt'
if (Test-Path $versionFile) { Report 'Unity version' ((Get-Content $versionFile -TotalCount 1) -join '') }
else { Report 'Unity version' 'UNKNOWN: ProjectVersion.txt missing' }
$manifest = Join-Path $project 'Packages\manifest.json'
if (Test-Path $manifest) {
    try {
        $json = Get-Content $manifest -Raw | ConvertFrom-Json
        $keys = @($json.dependencies.PSObject.Properties.Name)
        foreach ($id in @('com.unity.render-pipelines.universal','com.unity.probuilder','com.unity.cloud.gltfast','com.coplaydev.unity-mcp')) {
            $found = if ($keys -contains $id) { [string]$json.dependencies.PSObject.Properties[$id].Value } else { 'NOT DECLARED' }
            Report $id $found
        }
        $gitDeps = @($json.dependencies.PSObject.Properties | Where-Object { [string]$_.Value -match 'CoplayDev/unity-mcp' })
        if ($gitDeps.Count -gt 0) { Report 'MCP Git dependency' 'FOUND' }
    } catch { Report 'Package manifest' ("PARSE FAILED: " + $_.Exception.Message) }
} else { Report 'Package manifest' 'MISSING' }
$scenes = @(Get-ChildItem -Path (Join-Path $project 'Assets') -Filter '*.unity' -Recurse -File -ErrorAction SilentlyContinue)
$metas = @(Get-ChildItem -Path (Join-Path $project 'Assets') -Filter '*.meta' -Recurse -File -ErrorAction SilentlyContinue)
Report 'Unity scene files' ([string]$scenes.Count)
Report 'Unity meta files' ([string]$metas.Count)
$plan = Join-Path $project 'Assets\Case464\Data\room_profiles_24.v1.json'
Report '24-room art plan' $(if (Test-Path $plan) { 'PRESENT' } else { 'MISSING' })
$scriptPath = Join-Path $project 'Assets\Editor\Case464MapWorkshop.cs'
Report 'Map workshop script' $(if (Test-Path $scriptPath) { 'PRESENT (UNCOMPILED)' } else { 'MISSING' })
$cc0 = Join-Path $project 'Assets\Case464\Art\Environment'
$models = if (Test-Path $cc0) { @(Get-ChildItem $cc0 -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in '.obj','.glb' }).Count } else { 0 }
Report 'Staged source 3D files' ("$models (not approved as finished art)")
Report 'MCP live editor handshake' 'UNVERIFIED: confirm in Unity MCP window and MCP client'
Report 'Visual/art quality' 'UNVERIFIED: requires in-editor visual review'
Report 'Play Mode / build' 'NOT REQUESTED; NOT RUN'
Write-Host 'STOP on missing project/connection/assets. Do not pretend the running editor is linked.'
