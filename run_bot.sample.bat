@echo off
setlocal enabledelayedexpansion
title TadBot Launcher

REM ============================
REM PUT YOUR TOKEN HERE
REM ============================
set "DISCORD_BOT_TOKEN=PASTE_TOKEN_HERE"

set "LAUNCHER_DIR=%~dp0"
set "APP_DIR=%LAUNCHER_DIR%app"

REM Run updater first (safe even if already up to date)
call "%LAUNCHER_DIR%update.bat"
if %errorlevel% neq 0 (
  echo [FATAL] update.bat failed. Not starting bot.
  pause
  exit /b 1
)

cd /d "%APP_DIR%"

REM Install deps if needed
if not exist "node_modules" (
  echo [INFO] Installing dependencies...
  if exist package-lock.json (
    call npm ci
  ) else (
    call npm install
  )
)

REM Supervisor loop
:run
echo.
echo [INFO] Starting bot...
node src\index.js
set "EXITCODE=%errorlevel%"

if "%EXITCODE%"=="99" (
  echo [INFO] Bot requested shutdown. Stopping launcher.
  exit /b 0
)

echo [WARN] Bot exited (code=%EXITCODE%). Restarting in 2 seconds...
timeout /t 2 >nul
goto run
