@echo off
setlocal

set BRANCH=main

if "%~1"=="" (
    set COMMIT_MSG=build: update build output
) else (
    set COMMIT_MSG=%*
)

echo ===> Build project
dotnet publish -p:NoWarn="*" -c Release -r linux-x64 --no-self-contained -o .\bin\MTS-Server-Build
if errorlevel 1 exit /b 1

echo ===> Move to build folder
cd /d .\bin\MTS-Server-Build
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