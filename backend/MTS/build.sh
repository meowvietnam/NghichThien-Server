#!/bin/bash
set -e
BRANCH="main"
COMMIT_MSG=${1:-"build: update build output"}
echo "===> Build project"
dotnet publish -p:NoWarn="*" -c Release -r linux-x64 --no-self-contained -o ./bin/Build
echo "===> Move to build folder"
cd ./bin/Build
echo "===> Git add"
git add .
echo "===> Commit"
git commit -m "$COMMIT_MSG" || echo "Nothing to commit"
echo "===> Push"
git push origin $BRANCH
echo "===> Done!"