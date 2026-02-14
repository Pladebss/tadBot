@echo off
setlocal enabledelayedexpansion
title TadBot Launcher

REM ============================================================
REM 1) SET YOUR BOT TOKEN BELOW
REM    (This file stays local. Do NOT commit with real token.)
REM ============================================================
set "DISCORD_BOT_TOKEN=PASTE_TOKEN_HERE"

cd /d "%~dp0"

echo.
echo [INFO] TadBot Launcher
echo [INFO] Directory: %cd%

REM ============================================================
REM 2) Basic sanity checks
REM ============================================================

if not exist "TadBot.exe" (
  echo.
  echo [FATAL] TadBot.exe not found in this folder.
  echo Make sure run.bat is next to TadBot.exe.
  pause
  exit /b 1
)

if not exist "config.json" (
  echo.
  echo [FATAL] config.json not found.
  echo Copy config.template.json to config.json and configure it.
  pause
  exit /b 1
)

echo %DISCORD_BOT_TOKEN% | findstr /i "PASTE_TOKEN_HERE" >nul
if %errorlevel%==0 (
  echo.
  echo [FATAL] You forgot to paste your bot token.
  pause
  exit /b 1
)

REM ============================================================
REM 3) Supervisor loop (auto-restart support)
REM ============================================================

:run
echo.
echo [INFO] Starting TadBot.exe...
TadBot.exe

set "EXITCODE=%errorlevel%"

if "%EXITCODE%"=="99" (
  echo.
  echo [INFO] Bot requested shutdown (exit code 99).
  exit /b 0
)

echo.
echo [WARN] Bot exited (code=%EXITCODE%). Restarting in 2 seconds...
timeout /t 2 >nul
goto run
