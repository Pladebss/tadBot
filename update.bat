@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

REM ============================
REM Settings
REM ============================
set "OWNER=Pladebss"
set "REPO=tadBot"
set "ASSET_NAME=tadBot.zip"

set "LAUNCHER_DIR=%~dp0"
set "APP_DIR=%LAUNCHER_DIR%app"
set "VERSION_FILE=%LAUNCHER_DIR%app_version.txt"
set "TEMP_DIR=%LAUNCHER_DIR%temp_update"

if not exist "%VERSION_FILE%" (
  echo none>"%VERSION_FILE%"
)

for /f "usebackq delims=" %%v in ("%VERSION_FILE%") do set "LOCAL_VER=%%v"

echo [INFO] Local version: %LOCAL_VER%

REM ============================
REM Query GitHub latest release
REM ============================
for /f "usebackq delims=" %%A in (`powershell -NoProfile -Command ^
  "$u='https://api.github.com/repos/%OWNER%/%REPO%/releases/latest';" ^
  "$r=Invoke-RestMethod -Uri $u -Headers @{ 'User-Agent'='tadbot-updater' };" ^
  "$tag=$r.tag_name;" ^
  "$asset=$r.assets | Where-Object { $_.name -eq '%ASSET_NAME%' } | Select-Object -First 1;" ^
  "if(-not $asset){ throw 'Missing asset: %ASSET_NAME%' }" ^
  "$dl=$asset.browser_download_url;" ^
  "Write-Output ($tag + '|' + $dl)"`) do set "LINE=%%A"

for /f "tokens=1,2 delims=|" %%a in ("%LINE%") do (
  set "LATEST_VER=%%a"
  set "DL_URL=%%b"
)

echo [INFO] Latest version: %LATEST_VER%
if /i "%LOCAL_VER%"=="%LATEST_VER%" (
  echo [INFO] Already up-to-date. Skipping download.
  exit /b 0
)

echo [INFO] Update required -> %LATEST_VER%

REM ============================
REM Prep temp folder
REM ============================
if exist "%TEMP_DIR%" rd /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%" >nul

REM ============================
REM Backup preserved files
REM ============================
set "PRESERVE_CFG=%TEMP_DIR%\preserve_config.json"
set "PRESERVE_STATE=%TEMP_DIR%\preserve_state.json"

if exist "%APP_DIR%\config.json" (
  copy /y "%APP_DIR%\config.json" "%PRESERVE_CFG%" >nul
)

if exist "%APP_DIR%\data\state.json" (
  copy /y "%APP_DIR%\data\state.json" "%PRESERVE_STATE%" >nul
)

REM ============================
REM Download asset
REM ============================
set "ZIP_PATH=%TEMP_DIR%\%ASSET_NAME%"

echo [INFO] Downloading %ASSET_NAME%...
powershell -NoProfile -Command ^
  "$ProgressPreference='SilentlyContinue';" ^
  "Invoke-WebRequest -Uri '%DL_URL%' -OutFile '%ZIP_PATH%'" || (
    echo [FATAL] Download failed.
    exit /b 1
)

REM ============================
REM Extract
REM ============================
set "EXTRACT_DIR=%TEMP_DIR%\extract"
mkdir "%EXTRACT_DIR%" >nul

powershell -NoProfile -Command ^
  "Expand-Archive -Path '%ZIP_PATH%' -DestinationPath '%EXTRACT_DIR%' -Force" || (
    echo [FATAL] Extract failed.
    exit /b 1
)

REM ============================
REM Find bot root inside extracted folder
REM (handles both flat zips and GitHub nested zips)
REM ============================
set "NEW_ROOT=%EXTRACT_DIR%"

REM If the zip contains a single top-level folder, use it.
for /f "usebackq delims=" %%D in (`powershell -NoProfile -Command ^
  "$dirs=Get-ChildItem -Path '%EXTRACT_DIR%' -Directory;" ^
  "if($dirs.Count -eq 1){ $dirs[0].FullName } else { '%EXTRACT_DIR%' }"`) do set "NEW_ROOT=%%D"

REM Validate it looks like a node app (package.json must exist)
if not exist "%NEW_ROOT%\package.json" (
  echo [FATAL] package.json not found in extracted content.
  echo [HINT] Make sure %ASSET_NAME% contains the bot at zip root.
  exit /b 1
)

REM ============================
REM Replace app folder (atomic-ish swap)
REM ============================
set "OLD_APP=%LAUNCHER_DIR%app_old"
if exist "%OLD_APP%" rd /s /q "%OLD_APP%"

if exist "%APP_DIR%" (
  ren "%APP_DIR%" "app_old"
)

mkdir "%APP_DIR%" >nul

echo [INFO] Copying new version into app\ ...
robocopy "%NEW_ROOT%" "%APP_DIR%" /E /NFL /NDL /NJH /NJS >nul

REM ============================
REM Restore preserved files
REM ============================
if exist "%PRESERVE_CFG%" (
  echo [INFO] Restoring preserved config.json
  copy /y "%PRESERVE_CFG%" "%APP_DIR%\config.json" >nul
)

if exist "%PRESERVE_STATE%" (
  echo [INFO] Restoring preserved data\state.json
  if not exist "%APP_DIR%\data" mkdir "%APP_DIR%\data" >nul
  copy /y "%PRESERVE_STATE%" "%APP_DIR%\data\state.json" >nul
)

REM ============================
REM Install deps (inside app)
REM ============================
echo [INFO] Installing dependencies...
pushd "%APP_DIR%"
if exist package-lock.json (
  call npm ci
) else (
  call npm install
)
popd

REM ============================
REM Write new version + cleanup
REM ============================
echo %LATEST_VER%>"%VERSION_FILE%"

echo [INFO] Update complete. Now on %LATEST_VER%

REM Optional: remove old app backup after success
if exist "%OLD_APP%" rd /s /q "%OLD_APP%"

if exist "%TEMP_DIR%" rd /s /q "%TEMP_DIR%"

exit /b 0
