@echo off
setlocal EnableDelayedExpansion
title Build botcore.exe

set "ROOT=%~dp0"
cd /d "%ROOT%"

set "OUTDIR=%ROOT%executable"
set "EXE=botcore.exe"
set "ENTRY=src/index.js"

where node >nul 2>nul || (echo [FATAL] Node not found & pause & exit /b 1)
where npm  >nul 2>nul || (echo [FATAL] npm not found & pause & exit /b 1)

echo [INFO] Installing deps...
if exist package-lock.json (
  call npm ci || (echo [FATAL] npm ci failed & pause & exit /b 1)
) else (
  call npm install || (echo [FATAL] npm install failed & pause & exit /b 1)
)

echo [INFO] Ensuring pkg...
call npx --yes pkg --version >nul 2>nul || (echo [FATAL] pkg not available & pause & exit /b 1)

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

echo [INFO] Building %EXE% ...
call npx --yes pkg "%ENTRY%" --targets node18-win-x64 --output "%OUTDIR%\%EXE%"
if errorlevel 1 (
  echo [FATAL] pkg build failed
  pause
  exit /b 1
)

echo [DONE] %OUTDIR%\%EXE%
exit /b 0
