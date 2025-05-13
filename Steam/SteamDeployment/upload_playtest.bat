@echo off
setlocal

:: Capture the directory where this script lives
set SCRIPT_DIR=%~dp0

:: Define the parent build folder
set PARENT_DIR=C:\Users\%USERNAME%\Documents\Superspective\PlaytestBuilds

:: Find the most recently updated subfolder (safely captured)
for /f "usebackq delims=" %%A in (`powershell -NoProfile -Command "(Get-ChildItem -Directory -Path '%PARENT_DIR%' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Select-Object -ExpandProperty Name)"`) do set DEFAULT_BUILD_FOLDER=%%A

echo Detected latest build folder: %DEFAULT_BUILD_FOLDER%

:: Prompt for Build Folder (prefilled)
for /f "delims=" %%A in ('powershell -Command "[void][Reflection.Assembly]::LoadWithPartialName('Microsoft.VisualBasic'); [Microsoft.VisualBasic.Interaction]::InputBox('Enter the build folder name' + [Environment]::NewLine + 'Example: SuperspectivePlaytest_5-5-25', 'Build Folder', '%DEFAULT_BUILD_FOLDER%')"') do set BUILD_FOLDER=%%A

if "%BUILD_FOLDER%"=="" (
    echo No build folder entered. Exiting.
    exit /b 1
)

:: Prompt for Version Description
for /f "delims=" %%A in ('powershell -Command "[void][Reflection.Assembly]::LoadWithPartialName('Microsoft.VisualBasic'); [Microsoft.VisualBasic.Interaction]::InputBox('Enter the version number' + [Environment]::NewLine + 'Example: 0.1.0', 'Version Number', '')"') do set VERSION_DESC=%%A

if "%VERSION_DESC%"=="" (
    echo No version description entered. Exiting.
    exit /b 1
)

set FULL_BUILD_PATH=%PARENT_DIR%\%BUILD_FOLDER%

echo --------------------------------------------------
echo BUILD_FOLDER: %BUILD_FOLDER%
echo VERSION_DESC: %VERSION_DESC%
echo FULL_BUILD_PATH: %FULL_BUILD_PATH%
echo SCRIPT_DIR: %SCRIPT_DIR%
echo --------------------------------------------------

:: Replace placeholder in playtest depot VDF
echo Creating playtest_windows_depot.vdf...
type "%SCRIPT_DIR%playtest_windows_depot.template.vdf" > "%SCRIPT_DIR%playtest_windows_depot.vdf"
powershell -Command "(Get-Content '%SCRIPT_DIR%playtest_windows_depot.vdf').Replace('__BUILD_PATH__', '%FULL_BUILD_PATH%') | Set-Content '%SCRIPT_DIR%playtest_windows_depot.vdf'"
echo playtest_windows_depot.vdf complete!

:: Replace placeholder in playtest app build VDF
echo Creating playtest_app_build.vdf...
type "%SCRIPT_DIR%playtest_app_build.template.vdf" > "%SCRIPT_DIR%playtest_app_build.vdf"
powershell -Command "(Get-Content '%SCRIPT_DIR%playtest_app_build.vdf').Replace('__VERSION_DESC__', '%VERSION_DESC%') | Set-Content '%SCRIPT_DIR%playtest_app_build.vdf'"
echo playtest_app_build.vdf complete!

:: Run SteamCMD upload for Playtest
echo Running Steam upload for Playtest (you may be prompted to login)
C:\steamcmd\steamcmd.exe +login goshjosh182 +run_app_build "%SCRIPT_DIR%playtest_app_build.vdf" +quit

if %ERRORLEVEL% neq 0 (
    echo Upload failed.
    pause
    exit /b %ERRORLEVEL%
)

echo Upload completed successfully.
pause
endlocal
