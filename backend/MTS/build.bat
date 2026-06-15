@echo off
setlocal

set BRANCH=main
set BUILD_DIR=.\bin\MTS-Server-Build

if "%~1"=="" (
    set COMMIT_MSG=build: update build output
) else (
    set COMMIT_MSG=%*
)

echo ===> Prepare build folder
if not exist "%BUILD_DIR%\.git" (
    echo Build folder is not a Git repository: %BUILD_DIR%
    exit /b 1
)
git -C "%BUILD_DIR%" reset --hard
if errorlevel 1 exit /b 1
git -C "%BUILD_DIR%" clean -fd
if errorlevel 1 exit /b 1
git -C "%BUILD_DIR%" pull origin %BRANCH%
if errorlevel 1 exit /b 1

echo ===> Build project
dotnet build MTS.sln -p:TreatWarningsAsErrors=false -p:WarningLevel=0 -p:NoWarn="*" -clp:ErrorsOnly -c Release
if errorlevel 1 exit /b 1
dotnet publish MTS.csproj -p:BuildProjectReferences=true -p:TreatWarningsAsErrors=false -p:WarningLevel=0 -p:NoWarn="*" -clp:ErrorsOnly -c Release -r linux-x64 --no-self-contained -o "%BUILD_DIR%"
if errorlevel 1 exit /b 1

echo ===> Move to build folder
cd /d "%BUILD_DIR%"
if errorlevel 1 exit /b 1

echo ===> Git add
git add .

echo ===> Commit
git commit -m "%COMMIT_MSG%"
if errorlevel 1 (
    echo Nothing to commit
)

echo ===> Push
git push origin %BRANCH%
if errorlevel 1 exit /b 1

echo ===> Done!

endlocal
