@echo off
setlocal EnableDelayedExpansion
title TadSync - Build Launcher

echo ============================================
echo          TadSync - Build Launcher
echo ============================================

set "REPO=%~dp0"
set "PROJ=%REPO%launcher\TadSyncLauncher\TadSyncLauncher.csproj"

set "OUTROOT=%REPO%executable"
set "OUTLAUNCH=%OUTROOT%\launcher"
set "PUBLISH=%REPO%temp_publish_launcher"

set "EXE_NAME=TadSyncLauncher.exe"

echo [INFO] Repo   : %REPO%
echo [INFO] Project: %PROJ%
echo [INFO] Out    : %OUTLAUNCH%
echo.

if not exist "%PROJ%" (
  echo [FATAL] Launcher project not found:
  echo        %PROJ%
  pause
  exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [FATAL] dotnet not found in PATH.
  echo Install .NET SDK 8 and reopen terminal.
  pause
  exit /b 1
)

REM ============================================================
REM Clean output folders
REM ============================================================
if exist "%PUBLISH%" rmdir /s /q "%PUBLISH%"
if exist "%OUTLAUNCH%" rmdir /s /q "%OUTLAUNCH%"

mkdir "%PUBLISH%" >nul 2>nul
mkdir "%OUTLAUNCH%" >nul 2>nul

REM ============================================================
REM Publish (true single-file) to temp folder
REM ============================================================
echo [INFO] Publishing single-file launcher...
dotnet publish "%PROJ%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -o "%PUBLISH%" ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false ^
  /p:PublishTrimmed=false

if errorlevel 1 (
  echo [FATAL] dotnet publish failed
  pause
  exit /b 1
)

REM ============================================================
REM Copy ONLY the exe we care about
REM ============================================================
if not exist "%PUBLISH%\%EXE_NAME%" (
  echo [FATAL] Expected launcher exe not found:
  echo        %PUBLISH%\%EXE_NAME%
  echo.
  echo Files produced:
  dir /b "%PUBLISH%"
  pause
  exit /b 1
)

copy /y "%PUBLISH%\%EXE_NAME%" "%OUTLAUNCH%\%EXE_NAME%" >nul
if errorlevel 1 (
  echo [FATAL] Failed to copy launcher exe to output.
  pause
  exit /b 1
)

REM ============================================================
REM Delete temp publish folder to prevent "file dump"
REM ============================================================
rmdir /s /q "%PUBLISH%"

echo.
echo [DONE] Launcher built:
echo   %OUTLAUNCH%\%EXE_NAME%
echo.
pause
exit /b 0
