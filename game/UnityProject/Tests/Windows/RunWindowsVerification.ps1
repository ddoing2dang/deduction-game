param(
 [Parameter(Mandatory=$true)][string]$UnityPath,
 [Parameter(Mandatory=$true)][string]$PythonPath,
 [string]$SourceProject=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
 [string]$OutputRoot
)
$ErrorActionPreference='Stop'
if(-not $OutputRoot){$OutputRoot=[IO.Path]::GetFullPath((Join-Path $SourceProject '../../../Builds/Windows'))}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$running=@(Get-Process -ErrorAction SilentlyContinue | Where-Object {try {$_.Path -and $_.Path.StartsWith($OutputRoot,[StringComparison]::OrdinalIgnoreCase)}catch{$false}})
if($running.Count -gt 0){throw "Close running verification builds before rebuilding: $($running.ProcessName -join ', ')"}
$copy=Join-Path $env:TEMP ('DeductionGameWindows-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $copy | Out-Null
foreach($folder in @('Assets','Packages','ProjectSettings')){Copy-Item -LiteralPath (Join-Path $SourceProject $folder) -Destination $copy -Recurse}
& $PythonPath (Join-Path $PSScriptRoot 'PreparePlayerTests.py') --source $SourceProject --target $copy
if($LASTEXITCODE -ne 0){throw 'Test-copy preparation failed'}
function Run-Checked([string]$executable,[string]$arguments,[string]$log,[int]$timeout,[string]$marker){
 $process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $deadline=[DateTime]::UtcNow.AddSeconds($timeout)
 while(-not $process.WaitForExit(1000)){if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $process.Id;throw "Timeout: $log"}}
 $process.Refresh()
 if($process.ExitCode -ne 0 -or -not(Select-String -LiteralPath $log -SimpleMatch $marker)){throw "Failed: $log (exit $($process.ExitCode))"}
 Write-Output "OK $log"
}
$development=Join-Path $OutputRoot 'Development'
$buildLog=Join-Path $copy 'BuildDevelopment.log'
Run-Checked $UnityPath "-batchmode -nographics -quit -projectPath `"$copy`" -executeMethod WindowsBuildVerification.BuildDevelopment --build-output=`"$development`" -logFile `"$buildLog`"" $buildLog 600 'WINDOWS_BUILD: OK Museum'
foreach($theme in @('Mansion','Cruise','Museum')){
 foreach($mode in @('loop','ui','display')){
  $exe=Join-Path $development "$theme/DeductionGame.exe";$log=Join-Path $copy "$theme-$mode.log"
  $marker=switch($mode){'loop'{'SINGLE_PLAYER_E2E: OK'};'ui'{'USABILITY_VERIFICATION: OK'};'display'{'PLAYER_DISPLAY_VERIFICATION: OK'}}
  Run-Checked $exe "--verify-$mode -logFile `"$log`"" $log 120 $marker
 }
}
$release=Join-Path $OutputRoot 'Release'
$releaseLog=Join-Path $copy 'BuildRelease.log'
Run-Checked $UnityPath "-batchmode -nographics -quit -projectPath `"$copy`" -executeMethod WindowsBuildVerification.BuildRelease --build-output=`"$release`" -logFile `"$releaseLog`"" $releaseLog 600 'WINDOWS_BUILD: OK Museum'
Write-Output "Windows verification artifacts: $OutputRoot"
Write-Output "Windows verification logs preserved: $copy"
