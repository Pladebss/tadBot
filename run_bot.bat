@echo off
setlocal enabledelayedexpansion
title TadBot Launcher

REM ============================================================
REM 1) SET YOUR TOKEN HERE (KEEP THIS FILE LOCAL)
REM ============================================================
set "DISCORD_BOT_TOKEN=PASTE_TOKEN_HERE"

set "REPO_DIR=%~dp0"
cd /d "%REPO_DIR%"

echo.
echo [INFO] Repo dir: %REPO_DIR%

REM ============================================================
REM 2) Sanity checks (so it doesn't silently fail)
REM ============================================================
where node >nul 2>nul
if %errorlevel% neq 0 (
  echo [FATAL] Node.js not found in PATH.
  echo Install Node.js LTS and reopen this window.
  pause
  exit /b 1
)

where npm >nul 2>nul
if %errorlevel% neq 0 (
  echo [FATAL] npm not found in PATH.
  echo Install Node.js LTS and reopen this window.
  pause
  exit /b 1
)

if not exist "package.json" (
  echo [FATAL] package.json not found. Put run_bot.bat in the repo root.
  pause
  exit /b 1
)

if not exist "src\index.js" (
  echo [FATAL] src\index.js not found. Check your folder structure.
  pause
  exit /b 1
)

if not exist "config.json" (
  echo [FATAL] config.json not found.
  echo Copy config.template.json to config.json and fill it out.
  pause
  exit /b 1
)

echo %DISCORD_BOT_TOKEN% | findstr /i "PASTE_TOKEN_HERE" >nul
if %errorlevel%==0 (
  echo [FATAL] You forgot to paste your bot token into DISCORD_BOT_TOKEN.
  pause
  exit /b 1
)

REM ============================================================
REM 3) Install deps (only if needed)
REM ============================================================
echo.
echo [INFO] Ensuring dependencies...
if not exist "node_modules\" (
  if exist package-lock.json (
    call npm ci
  ) else (
    call npm install
  )
  if %errorlevel% neq 0 (
    echo [FATAL] Dependency install failed.
    pause
    exit /b 1
  )
)

REM ============================================================
REM 4) Supervisor loop (auto-restart bot)
REM ============================================================
:run
echo.
echo [INFO] Starting bot...
node src\index.js

set "EXITCODE=%errorlevel%"

if "%EXITCODE%"=="99" (
  echo.
  echo [INFO] Bot requested shutdown (exit code 99). Stopping launcher.
  exit /b 0
)

echo.
echo [WARN] Bot exited (code=%EXITCODE%). Restarting in 2 seconds...
timeout /t 2 >nul
goto run
