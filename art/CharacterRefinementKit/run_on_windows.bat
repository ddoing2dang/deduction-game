@echo off
setlocal
REM Requires Blender 4.4 or later in PATH (or replace blender with full Blender.exe path).
set "BLENDER=blender"
set "SCRIPT=%~dp0prepare_characters.py"
set "ROOT=%~dp0sources"
set "OUT=%~dp0refinement_work"
if not exist "%OUT%" mkdir "%OUT%"
for %%C in (BOKI MATTO JIRO NAZ MARIHI MUNIRE) do (
  if exist "%ROOT%\%%C.blend" (
    "%BLENDER%" -b "%ROOT%\%%C.blend" --python "%SCRIPT%" -- --character %%C --output "%OUT%"
  ) else (
    echo Missing: "%ROOT%\%%C.blend"
  )
)
echo.
echo NOTE: PREP only. The geometry has NOT been sculpted. Details remain to be authored manually in Blender.
pause
