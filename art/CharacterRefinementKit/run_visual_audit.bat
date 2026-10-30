@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "ROOT=%~dp0"
set "BLENDER_EXE="
REM Prefer blender location from PATH or standard installation folders.
for /f "delims=" %%P in ('where blender.exe 2^>nul') do if not defined BLENDER_EXE set "BLENDER_EXE=%%~fP"
if not defined BLENDER_EXE if exist "%ProgramFiles%\Blender Foundation\Blender\blender.exe" set "BLENDER_EXE=%ProgramFiles%\Blender Foundation\Blender\blender.exe"
if not defined BLENDER_EXE for /d %%D in ("%ProgramFiles%\Blender Foundation\Blender *") do if exist "%%~fD\blender.exe" set "BLENDER_EXE=%%~fD\blender.exe"
if not defined BLENDER_EXE for /d %%D in ("%LOCALAPPDATA%\Programs\Blender Foundation\Blender *") do if exist "%%~fD\blender.exe" set "BLENDER_EXE=%%~fD\blender.exe"
if not defined BLENDER_EXE if exist "%ProgramFiles(x86)%\Steam\steamapps\common\Blender\blender.exe" set "BLENDER_EXE=%ProgramFiles(x86)%\Steam\steamapps\common\Blender\blender.exe"
if not defined BLENDER_EXE (
    echo Blender is not detected. Choose blender.exe in the picker.
    for /f "usebackq delims=" %%P in (`powershell.exe -NoProfile -STA -Command "Add-Type -AssemblyName System.Windows.Forms; $dlg=New-Object System.Windows.Forms.OpenFileDialog; $dlg.Title='Select Blender blender.exe'; $dlg.Filter='Blender executable|blender.exe'; if($dlg.ShowDialog() -eq 'OK'){$dlg.FileName}"`) do set "BLENDER_EXE=%%P"
)
if not defined BLENDER_EXE (
    echo [ERROR] Blender executable not found. Nothing has changed.
    pause
    exit /b 1
)
if not exist "%ROOT%render_character_views.py" (
    echo [ERROR] Put render_character_views.py beside this BAT in CharacterRefinementKit.
    pause
    exit /b 1
)
set "OUT=%ROOT%visual_audit"
if not exist "%OUT%" mkdir "%OUT%"
set "FAIL=0"
for %%C in (BOKI JIRO MARIHI MATTO MUNIRE NAZ) do (
    set "INPUT=%ROOT%refinement_work\CHR_%%C_REFINEMENT_WIP.blend"
    if not exist "!INPUT!" set "INPUT=%ROOT%sources\%%C.blend"
    if not exist "!INPUT!" (
        echo [MISSING] %%C - WIP and original missing
        set "FAIL=1"
    ) else (
        echo.
        echo ==== Rendering diagnostic views of %%C ====
        "%BLENDER_EXE%" -b "!INPUT!" --python "%ROOT%render_character_views.py" -- --character %%C --output "%OUT%"
        if errorlevel 1 (
            echo [FAILED] %%C
            set "FAIL=1"
        )
    )
)
if "%FAIL%"=="0" (
    echo.
    echo ==== Creating one ZIP of the completed visual inspection ====
    powershell.exe -NoProfile -Command "Compress-Archive -Path '%OUT%\*' -DestinationPath '%ROOT%CharacterVisualAudit.zip' -Force"
    if errorlevel 1 (
        echo [WARNING] Images saved, but ZIP failed. Find them under visual_audit.
    ) else (
        echo [DONE] %ROOT%CharacterVisualAudit.zip
    )
    echo This is a visual DIAGNOSTIC. Facial sculpt, eye materials, UV and rigging are NOT completed.
) else (
    echo.
    echo [INCOMPLETE] See errors above. Existing files are unchanged.
)
pause
exit /b %FAIL%
