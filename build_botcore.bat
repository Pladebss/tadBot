@echo off
setlocal
title TadSync - Build Bot Core

set "ROOT=%~dp0"
set "OUT=%ROOT%launcher\TadSyncLauncher\Assets"
set "BOT_EXE_NAME=botcore.exe"

where node >nul 2>nul || (echo [FATAL] Node.js not found.& pause & exit /b 1)
where npm  >nul 2>nul || (echo [FATAL] npm not found.& pause & exit /b 1)

if not exist "%OUT%" mkdir "%OUT%"

cd /d "%ROOT%"
if exist package-lock.json (
  call npm ci || (echo [FATAL] npm ci failed.& pause & exit /b 1)
) else (
  call npm install || (echo [FATAL] npm install failed.& pause & exit /b 1)
)

call npx --yes pkg "src/index.js" --targets node18-win-x64 --output "%OUT%\%BOT_EXE_NAME%"
if errorlevel 1 (
  echo [FATAL] pkg build failed.
  pause
  exit /b 1
)

echo [DONE] Built: %OUT%\%BOT_EXE_NAME%
pause
