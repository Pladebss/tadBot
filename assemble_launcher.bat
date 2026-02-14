@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

set "HERE=%~dp0"
set "LAUNCHER=%HERE%TadBotLauncher"
set "APP=%LAUNCHER%\app"

if not exist "%HERE%package.json" (
  echo [FATAL] package.json not found here.
  echo Run this from the repo root (where package.json lives).
  pause
  exit /b 1
)

echo [INFO] Creating launcher structure...
if not exist "%LAUNCHER%" mkdir "%LAUNCHER%" >nul
if not exist "%APP%" mkdir "%APP%" >nul

echo [INFO] Copying bot files into TadBotLauncher\app\ ...
robocopy "%HERE%" "%APP%" /E /NFL /NDL /NJH /NJS ^
  /XD ".git" "node_modules" "TadBotLauncher" "temp_update" >nul

REM Ensure version file exists
if not exist "%LAUNCHER%\app_version.txt" (
  echo none>"%LAUNCHER%\app_version.txt"
)

REM Copy updater into launcher
if exist "%HERE%update.bat" (
  copy /y "%HERE%update.bat" "%LAUNCHER%\update.bat" >nul
)

REM Create run_bot.bat from sample if missing
if not exist "%LAUNCHER%\run_bot.bat" (
  if exist "%HERE%run_bot.sample.bat" (
    copy /y "%HERE%run_bot.sample.bat" "%LAUNCHER%\run_bot.bat" >nul
    echo [INFO] Created TadBotLauncher\run_bot.bat from sample.
  ) else (
    echo [WARN] run_bot.sample.bat not found. Create TadBotLauncher\run_bot.bat manually.
  )
)

REM Ensure config exists in app (copy from template if needed)
if not exist "%APP%\config.json" (
  if exist "%APP%\config.template.json" (
    copy /y "%APP%\config.template.json" "%APP%\config.json" >nul
    echo [INFO] Created app\config.json from config.template.json
  )
)

echo.
echo [DONE] Launcher is ready:
echo   %LAUNCHER%
echo.
echo Next steps:
echo 1) Open: %LAUNCHER%\run_bot.bat   (paste DISCORD_BOT_TOKEN)
echo 2) Edit: %LAUNCHER%\app\config.json
echo 3) Run:  %LAUNCHER%\run_bot.bat
echo.
pause
exit /b 0
