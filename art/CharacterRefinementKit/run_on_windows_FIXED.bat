@echo off
setlocal EnableExtensions
REM Character Refinement PREPARATION ONLY - no sculpting, texture painting or rigging.
REM This version finds Blender even when Blender is not in Windows PATH.
set "KIT_DIR=%~dp0"
set "BLENDER_EXE="

REM Optional: drag blender.exe onto this BAT to provide its location.
if not "%~1"=="" if exist "%~1" set "BLENDER_EXE=%~f1"

REM Search the normal command line PATH.
if not defined BLENDER_EXE for /f "delims=" %%P in ('where blender.exe 2^>nul') do if not defined BLENDER_EXE set "BLENDER_EXE=%%~fP"

REM Official Windows installer location (versioned directories).
if not defined BLENDER_EXE if exist "%ProgramFiles%\Blender Foundation\Blender\blender.exe" set "BLENDER_EXE=%ProgramFiles%\Blender Foundation\Blender\blender.exe"
if not defined BLENDER_EXE for /d %%D in ("%ProgramFiles%\Blender Foundation\Blender *") do if exist "%%~fD\blender.exe" set "BLENDER_EXE=%%~fD\blender.exe"

REM Per-user and Steam installation examples.
if not defined BLENDER_EXE for /d %%D in ("%LOCALAPPDATA%\Programs\Blender Foundation\Blender *") do if exist "%%~fD\blender.exe" set "BLENDER_EXE=%%~fD\blender.exe"
if not defined BLENDER_EXE if exist "%ProgramFiles(x86)%\Steam\steamapps\common\Blender\blender.exe" set "BLENDER_EXE=%ProgramFiles(x86)%\Steam\steamapps\common\Blender\blender.exe"

REM Portable Blender next to the extracted kit.
if not defined BLENDER_EXE if exist "%KIT_DIR%blender.exe" set "BLENDER_EXE=%KIT_DIR%blender.exe"
if not defined BLENDER_EXE if exist "%KIT_DIR%Blender\blender.exe" set "BLENDER_EXE=%KIT_DIR%Blender\blender.exe"

REM If not found, open an Explorer-style picker (does not require Blender in PATH).
if not defined BLENDER_EXE (
  echo [INFO] Blender was not found automatically.
  echo [INFO] Choose blender.exe in the file selection window.
  for /f "usebackq delims=" %%P in (`powershell.exe -NoProfile -STA -Command "Add-Type -AssemblyName System.Windows.Forms; $d = New-Object System.Windows.Forms.OpenFileDialog; $d.Title = 'Select blender.exe'; if ($d.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { $d.FileName }"`) do set "BLENDER_EXE=%%P"
)
if not defined BLENDER_EXE (
  echo [ERROR] Blender is missing or not selected.
  echo Install Blender 4.4+ or find blender.exe, then run this BAT again.
  echo No character files were changed.
  pause
  exit /b 1
)
if not exist "%BLENDER_EXE%" (
  echo [ERROR] Blender executable does not exist: "%BLENDER_EXE%"
  echo No character files were changed.
  pause
  exit /b 1
)

echo Using Blender: "%BLENDER_EXE%"
set "SCRIPT=%KIT_DIR%prepare_characters.py"
set "ROOT=%KIT_DIR%sources"
set "OUT=%KIT_DIR%refinement_work"
if not exist "%SCRIPT%" (
  echo [ERROR] prepare_characters.py is not in this folder.
  pause
  exit /b 1
)
if not exist "%OUT%" mkdir "%OUT%"
if not exist "%ROOT%" mkdir "%ROOT%"

set "FAIL=0"
for %%C in (BOKI MATTO JIRO NAZ MARIHI MUNIRE) do (
  if exist "%ROOT%\%%C.blend" (
    echo.
    echo ========================================
    echo Preparing %%C
    "%BLENDER_EXE%" -b "%ROOT%\%%C.blend" --python "%SCRIPT%" -- --character %%C --output "%OUT%"
    if errorlevel 1 (
      echo [FAILED] %%C - check the message above.
      set "FAIL=1"
    )
  ) else (
    echo [MISSING] "%ROOT%\%%C.blend"
    set "FAIL=1"
  )
)
echo.
if "%FAIL%"=="0" (
  echo [OK] All six characters PREPARED in refinement_work.
) else (
  echo [INCOMPLETE] Some characters were not prepared. See above.
)
echo [NOTE] PREP ONLY. No facial sculpting, iris assignment, UV painting, or rigging was performed.
echo [NOTE] Original files in sources are never overwritten.
pause
if "%FAIL%"=="0" (exit /b 0) else (exit /b 1)
