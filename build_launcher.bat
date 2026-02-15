@echo off
setlocal EnableDelayedExpansion
title Build TadSyncLauncher.exe

set "ROOT=%~dp0"
set "OUTDIR=%ROOT%executable"
set "LAUNCHER_PROJ=%ROOT%launcher\TadSyncLauncher\TadSyncLauncher.csproj"
set "ASSETS_DIR=%ROOT%launcher\TadSyncLauncher\Assets"
set "BOTCORE_EXE=%OUTDIR%\botcore.exe"

where dotnet >nul 2>nul || (echo [FATAL] dotnet not found & pause & exit /b 1)

if not exist "%BOTCORE_EXE%" (
  echo [FATAL] botcore.exe missing: %BOTCORE_EXE%
  echo Run build_botcore.bat first.
  pause
  exit /b 1
)

if not exist "%ASSETS_DIR%" mkdir "%ASSETS_DIR%"

echo [INFO] Copying botcore.exe into launcher assets...
copy /y "%BOTCORE_EXE%" "%ASSETS_DIR%\botcore.exe" >nul

echo [INFO] Publishing launcher (single exe)...
dotnet publish "%LAUNCHER_PROJ%" -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
  echo [FATAL] dotnet publish failed
  pause
  exit /b 1
)

set "PUBDIR=%ROOT%launcher\TadSyncLauncher\bin\Release\net8.0-windows\win-x64\publish"
if not exist "%PUBDIR%\TadSyncLauncher.exe" (
  echo [FATAL] Publish output not found at %PUBDIR%
  pause
  exit /b 1
)

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

echo [INFO] Copying final exe to executable\TadSyncLauncher.exe
copy /y "%PUBDIR%\TadSyncLauncher.exe" "%OUTDIR%\TadSyncLauncher.exe" >nul

echo [DONE] %OUTDIR%\TadSyncLauncher.exe
pause
exit /b 0
