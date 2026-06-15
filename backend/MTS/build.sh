#!/bin/bash
set -e
BRANCH="main"
BUILD_DIR="./bin/MTS-Server-Build"
COMMIT_MSG=${1:-"build: update build output"}

echo "===> Prepare build folder"
if [ ! -d "$BUILD_DIR/.git" ]; then
    echo "Build folder is not a Git repository: $BUILD_DIR"
    exit 1
fi
git -C "$BUILD_DIR" reset --hard
git -C "$BUILD_DIR" clean -fd
git -C "$BUILD_DIR" pull origin $BRANCH

echo "===> Build project"
dotnet build MTS.sln -p:TreatWarningsAsErrors=false -p:WarningLevel=0 -p:NoWarn="*" -clp:ErrorsOnly -c Release
dotnet publish MTS.csproj -p:BuildProjectReferences=true -p:TreatWarningsAsErrors=false -p:WarningLevel=0 -p:NoWarn="*" -clp:ErrorsOnly -c Release -r linux-x64 --no-self-contained -o "$BUILD_DIR"
echo "===> Move to build folder"
cd "$BUILD_DIR"
echo "===> Git add"
git add .
echo "===> Commit"
git commit -m "$COMMIT_MSG" || echo "Nothing to commit"
echo "===> Push"
git push origin $BRANCH
echo "===> Done!"
