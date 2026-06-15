@echo off
setlocal

:: 1. Định nghĩa chính xác vị trí các thư mục theo cấu trúc thực tế
set "BACKEND_DIR=D:\GitHub\WorkSpace\Server\MTS-Server\backend"
set "PROJECT_DIR=%BACKEND_DIR%\MTS"
set "BUILD_DIR=%PROJECT_DIR%\bin\MTS-Server-Build"
set BRANCH=main

if "%~1"=="" (
    set "COMMIT_MSG=build: update build output"
) else (
    set "COMMIT_MSG=%*"
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

echo ===> Build & Publish project
:: Trỏ thẳng vào file .csproj nằm ở thư mục MTS để build và publish luôn cho chắc cú
dotnet publish "%PROJECT_DIR%\MTS.csproj" -p:BuildProjectReferences=true -p:TreatWarningsAsErrors=false -p:WarningLevel=0 -p:NoWarn="*" -clp:ErrorsOnly -c Release -r linux-x64 --no-self-contained -o "%BUILD_DIR%"
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