param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][ValidateSet('SkyScreenshot','SkyAccessibility','HumanOnly')][string]$Mode,
 [Parameter(Mandatory=$true)][ValidateSet('X','AltF4')][string]$QuitMethod,
 [Parameter(Mandatory=$true)][string]$Trial,
 [string]$OutputRoot='C:/UnityProjects/DeductionGame/Builds/Windows/ExitVerification',
 [int]$TimeoutSeconds=180
)
$ErrorActionPreference='Stop'
if($Trial -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Trial must be a simple unique name'}
if(Get-Process DeductionGame -ErrorAction SilentlyContinue){throw 'Close the previous player first'}
$directory=Join-Path $OutputRoot $Trial
if(Test-Path -LiteralPath $directory){throw 'Do not overwrite an earlier trial'}
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$log=Join-Path $directory 'Player.log'
$start=Get-Date
$crashRoot='C:/Users/dm856/AppData/Local/Temp/ddoing2dang/Deduction Game/Crashes'
$oldCrashes=@(Get-ChildItem -LiteralPath $crashRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object Name)
# This collector never calls UIAutomation, sends input, or closes a window.
# HumanOnly is valid only when a human performs all window interaction.
$process=Start-Process -FilePath $Executable -ArgumentList "-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile `"$log`"" -WindowStyle Normal -PassThru
$handle=$process.Handle # Retain a handle before exit so ExitCode remains available.
Write-Output "READY $Trial PID=$($process.Id) mode=$Mode close=$QuitMethod"
$timedOut=-not $process.WaitForExit($TimeoutSeconds*1000)
if(-not $timedOut){$process.WaitForExit();$process.Refresh()}
Start-Sleep -Seconds 3
$content=if(Test-Path -LiteralPath $log){[IO.File]::ReadAllText($log)}else{''}
$newCrashes=@(Get-ChildItem -LiteralPath $crashRoot -Directory -ErrorAction SilentlyContinue | Where-Object {$_.Name -notin $oldCrashes} | ForEach-Object FullName)
$eventErrors=@()
$events=@(Get-WinEvent -FilterHashtable @{LogName='Application';StartTime=$start;Id=1000,1001} -ErrorAction SilentlyContinue -ErrorVariable eventErrors | Where-Object {$_.Message -match 'deductiongame.exe'} | ForEach-Object { [pscustomobject]@{RecordId=$_.RecordId;Id=$_.Id;Time=$_.TimeCreated.ToString('o');Message=$_.Message} })
$eventQueryOK=@($eventErrors | Where-Object {$_.FullyQualifiedErrorId -notlike 'NoMatchingEventsFound*'}).Count -eq 0
$result=[ordered]@{Trial=$Trial;Mode=$Mode;QuitMethod=$QuitMethod;Executable=$Executable;SHA256=(Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash;Started=$start.ToString('o');Ended=(Get-Date).ToString('o');PID=$process.Id;TimedOut=$timedOut;ExitCode=if($timedOut){$null}else{$process.ExitCode};ShutdownLogged=$content.Contains('Input System module state changed to: Shutdown.');FatalLog=($content -match 'Crash!!!|Unhandled Exception|\w+Exception:|Fatal error');NewCrashes=$newCrashes;WindowsEvents=$events;PlayerLog=$log}
$result.EventQueryOK=$eventQueryOK
$result.OK=(-not $timedOut -and $result.ExitCode -eq 0 -and $result.ShutdownLogged -and -not $result.FatalLog -and $newCrashes.Count -eq 0 -and $events.Count -eq 0 -and $eventQueryOK)
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $directory 'result.json') -Encoding utf8
Write-Output ($result | ConvertTo-Json -Depth 6 -Compress)
if(-not $result.OK){exit 1}
exit 0
