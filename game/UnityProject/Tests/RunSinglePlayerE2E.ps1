param(
    [Parameter(Mandatory=$true)][string]$UnityPath,
    [string]$SourceProject = (Split-Path $PSScriptRoot -Parent),
    [int]$TimeoutSeconds = 120
)
$ErrorActionPreference = 'Stop'
# No clean/reset, no writes to the source checkout. Each run owns this fresh copy.
$validationProject = Join-Path $env:TEMP ('DeductionGameE2E-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $validationProject | Out-Null
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $SourceProject $folder) -Destination $validationProject -Recurse
}
$cases = @('SinglePlayerE2E.Mansion','SinglePlayerE2E.Cruise','SinglePlayerE2E.Museum',
           'MansionEventVerification.Run','MapThemeVerification.Cruise','MapThemeVerification.Museum')
foreach ($case in $cases) {
    $log = Join-Path $validationProject ($case + '.log')
    $arguments = @('-batchmode','-nographics','-projectPath',('"' + $validationProject + '"'),'-executeMethod',$case,'-logFile',('"' + $log + '"'))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not $process.WaitForExit(1000)) {
        if ([DateTime]::UtcNow -gt $deadline) {
            Stop-Process -Id $process.Id
            throw "Timeout/deadlock: $case. Log: $log"
        }
    }
    $process.Refresh()
    $marker = if ($case.StartsWith('SinglePlayerE2E')) {'SINGLE_PLAYER_E2E: OK'} else {'USABILITY_VERIFICATION: OK'}
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch $marker)) {
        throw "Failed: $case. Log: $log"
    }
    Write-Output "OK $case"
}
Write-Output "Verification logs preserved: $validationProject"
