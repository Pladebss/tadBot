@echo off
setlocal EnableDelayedExpansion
title TadBot - Edit Token

cd /d "%~dp0"

if not exist "run.bat" (
  echo.
  echo [FATAL] run.bat not found in this folder.
  pause
  exit /b 1
)

echo.
echo ================================
echo        TadBot Token Editor
echo ================================
echo.
echo Paste your new Discord bot token below.
echo (Right-click to paste in this window.)
echo.

set /p NEWTOKEN=Enter new token: 

echo.
echo %NEWTOKEN% | findstr /i "PASTE_TOKEN_HERE" >nul
if %errorlevel%==0 (
  echo Invalid token entered.
  pause
  exit /b 1
)

if "%NEWTOKEN%"=="" (
  echo Token cannot be empty.
  pause
  exit /b 1
)

echo.
echo Updating run.bat...

set "TEMPFILE=run_tmp.bat"

(
for /f "usebackq delims=" %%A in ("run.bat") do (
  echo %%A | findstr /b /c:"set \"DISCORD_BOT_TOKEN=" >nul
  if !errorlevel! == 0 (
    echo set "DISCORD_BOT_TOKEN=!NEWTOKEN!"
  ) else (
    echo %%A
  )
)
) > "!TEMPFILE!"

move /y "!TEMPFILE!" "run.bat" >nul

echo.
echo Token successfully updated.
echo.
pause
