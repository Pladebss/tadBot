@echo off
setlocal EnableExtensions DisableDelayedExpansion
title TadSync - Build Launcher

set "ROOT=%~dp0"
set "OUT=%ROOT%executable"
set "LAUNCHER_EXE_NAME=TadSyncLauncher.exe"

where dotnet >nul 2>nul
if errorlevel 1 goto DOTNET_MISSING

if not exist "%OUT%" mkdir "%OUT%"

cd /d "%ROOT%launcher\TadSyncLauncher"
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 goto PUBLISH_FAIL

set "PUB=%ROOT%launcher\TadSyncLauncher\bin\Release\net8.0-windows\win-x64\publish"
if not exist "%PUB%\TadSyncLauncher.exe" goto EXE_MISSING

copy /y "%PUB%\TadSyncLauncher.exe" "%OUT%\%LAUNCHER_EXE_NAME%" >nul
if errorlevel 1 goto COPY_FAIL

echo.
echo [DONE] Built: %OUT%\%LAUNCHER_EXE_NAME%
echo.
pause
exit /b 0

:DOTNET_MISSING
echo [FATAL] dotnet SDK not found (install .NET 8 SDK).
pause
exit /b 1

:PUBLISH_FAIL
echo [FATAL] dotnet publish failed.
pause
exit /b 1

:EXE_MISSING
echo [FATAL] Launcher exe not found: %PUB%\TadSyncLauncher.exe
pause
exit /b 1

:COPY_FAIL
echo [FATAL] Failed to copy launcher exe.
pause
exit /b 1
