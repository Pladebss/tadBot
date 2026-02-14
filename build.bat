@echo off
setlocal EnableDelayedExpansion
title TadBot Build

REM =========================
REM Settings
REM =========================
set "OUTDIR=%~dp0executable"
set "EXENAME=TadBot.exe"
set "ENTRY=src/index.js"
set "TEMPLATE=%~dp0config.template.json"
set "RUN_TEMPLATE=%~dp0run.template.bat"
set "EDIT_TOKEN=%~dp0edit_token.bat"

echo.
echo ============================================
echo              TadBot Build
echo ============================================
echo.
echo [INFO] Repo: %~dp0
echo [INFO] Output: %OUTDIR%
echo.

REM =========================
REM Preflight checks
REM =========================
where node >nul 2>nul || (echo [FATAL] Node.js not found in PATH.& pause & exit /b 1)
where npm  >nul 2>nul || (echo [FATAL] npm not found in PATH.& pause & exit /b 1)

if not exist "%TEMPLATE%" (
  echo [FATAL] Missing config.template.json
  pause
  exit /b 1
)

if not exist "%RUN_TEMPLATE%" (
  echo [FATAL] Missing run.template.bat
  pause
  exit /b 1
)

if not exist "%EDIT_TOKEN%" (
  echo [FATAL] Missing edit_token.bat
  pause
  exit /b 1
)

REM =========================
REM Install dependencies
REM =========================
echo [INFO] Installing dependencies...
if exist "%~dp0package-lock.json" (
  call npm ci || (echo [FATAL] npm ci failed.& pause & exit /b 1)
) else (
  call npm install || (echo [FATAL] npm install failed.& pause & exit /b 1)
)

REM =========================
REM Ensure pkg is available
REM =========================
echo.
echo [INFO] Ensuring pkg is available...
call npx --yes pkg --version >nul 2>nul
if errorlevel 1 (
  echo [FATAL] pkg not available. Try: npm install -D pkg
  pause
  exit /b 1
)

REM =========================
REM Clean output folder
REM =========================
echo.
echo [INFO] Preparing output folder...
if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"
mkdir "%OUTDIR%" || (echo [FATAL] Could not create output folder.& pause & exit /b 1)

REM =========================
REM Build EXE
REM =========================
echo.
echo [INFO] Building %EXENAME% ...
call npx --yes pkg "%ENTRY%" --targets node18-win-x64 --output "%OUTDIR%\%EXENAME%"
if errorlevel 1 (
  echo [FATAL] pkg build failed.
  pause
  exit /b 1
)

REM =========================
REM Copy templates
REM =========================
echo.
echo [INFO] Copying config template...
copy /y "%TEMPLATE%" "%OUTDIR%\config.json" >nul

echo [INFO] Copying run template...
copy /y "%RUN_TEMPLATE%" "%OUTDIR%\run.bat" >nul

echo [INFO] Copying edit_token.bat...
copy /y "%EDIT_TOKEN%" "%OUTDIR%\edit_token.bat" >nul

echo [INFO] Copying tkn_gui.ps1...
copy /y "%~dp0tkn_gui.ps1" "%OUTDIR%\tkn_gui.ps1" >nul


REM =========================
REM Create data folder
REM =========================
mkdir "%OUTDIR%\data" >nul 2>nul

echo.
echo ============================================
echo [DONE] Build complete.
echo.
echo Distribution folder:
echo   %OUTDIR%
echo.
echo Next steps:
echo 1) Open executable\edit_token.bat
echo 2) Paste bot token
echo 3) Run executable\run.bat
echo ============================================
echo.
pause
exit /b 0
