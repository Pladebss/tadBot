
@echo off
setlocal enabledelayedexpansion
title Discord Booster Bot V3.1 Launcher

REM ============================================================
REM 1) SET YOUR TOKEN HERE (COPY THIS FILE TO run_bot.bat)
REM ============================================================
set "DISCORD_BOT_TOKEN=PASTE_TOKEN_HERE"

REM ============================================================
REM 2) OPTIONAL: Portable Git location (if git not installed)
REM    Place portable git under: tools\git\cmd\git.exe
REM ============================================================
set "REPO_DIR=%~dp0"
set "TOOLS_DIR=%REPO_DIR%tools"
set "PORTABLE_GIT_EXE=%TOOLS_DIR%\git\cmd\git.exe"

cd /d "%REPO_DIR%"

REM ============================================================
REM 3) Resolve git (system git preferred)
REM ============================================================
where git >nul 2>nul
if %errorlevel%==0 (
  set "GIT=git"
) else (
  if exist "%PORTABLE_GIT_EXE%" (
    set "GIT=%PORTABLE_GIT_EXE%"
  ) else (
    echo.
    echo [FATAL] Git not found.
    echo - Install Git for Windows, OR
    echo - Place portable git at: %PORTABLE_GIT_EXE%
    echo.
    pause
    exit /b 1
  )
)

REM ============================================================
REM 4) Update only if remote changed
REM ============================================================
echo.
echo [INFO] Checking for updates...
"%GIT%" fetch --quiet

for /f %%H in ('"%GIT%" rev-parse HEAD') do set "LOCAL=%%H"
for /f %%H in ('"%GIT%" rev-parse @{u}' ) do set "REMOTE=%%H"

if /I not "%LOCAL%"=="%REMOTE%" (
  echo [INFO] Update found - pulling latest...
  "%GIT%" pull --ff-only
) else (
  echo [INFO] No update - skipping pull.
)

REM ============================================================
REM 5) Install deps
REM ============================================================
echo.
echo [INFO] Installing dependencies...
if exist package-lock.json (
  call npm ci
) else (
  call npm install
)

REM ============================================================
REM 6) Supervisor loop (enables /restart bot)
REM ============================================================
:run
echo.
echo [INFO] Starting bot...
node src\index.js

echo.
echo [WARN] Bot exited. Restarting in 2 seconds...
timeout /t 2 >nul
goto run
