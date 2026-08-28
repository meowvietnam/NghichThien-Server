#!/bin/bash
set -e
BRANCH="main"
BUILD_DIR="./bin/MTS-Server-Build"
COMMIT_MSG=${1:-"build: update build output"}
DOTNET_PROPS=(-p:BuildProjectReferences=true -p:TreatWarningsAsErrors=false -p:WarningLevel=0 -p:NoWarn="*" -p:CopyLocalLockFileAssemblies=true -clp:ErrorsOnly)

echo "===> Prepare build folder"
if [ ! -d "$BUILD_DIR/.git" ]; then
    echo "Build folder is not a Git repository: $BUILD_DIR"
    exit 1
fi
git -C "$BUILD_DIR" reset --hard
git -C "$BUILD_DIR" clean -fdx
git -C "$BUILD_DIR" pull origin $BRANCH

echo "===> Restore dependencies"
dotnet restore ../MTS.slnx -p:BuildProjectReferences=true -r linux-x64
echo "===> Build project"
dotnet build ../MTS.slnx "${DOTNET_PROPS[@]}" --no-restore -c Release
echo "===> Publish project"
dotnet publish MTS.csproj "${DOTNET_PROPS[@]}" --no-restore -c Release -r linux-x64 --no-self-contained -o "$BUILD_DIR"
echo "===> Validate publish dependencies"
test -f "$BUILD_DIR/Microsoft.IdentityModel.Tokens.dll"
test -f "$BUILD_DIR/Microsoft.AspNetCore.Authentication.JwtBearer.dll"
test -f "$BUILD_DIR/MTS.deps.json"
echo "===> Move to build folder"
cd "$BUILD_DIR"
echo "===> Git add"
git add -A -f .
echo "===> Commit"
git commit -m "$COMMIT_MSG" || echo "Nothing to commit"
echo "===> Push"
git push origin $BRANCH
echo "===> Done!"
