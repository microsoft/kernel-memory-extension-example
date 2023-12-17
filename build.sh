#!/usr/bin/env bash

set -e

# Folder where the project is located
PROJECT="PostgresMemoryStorage"
PACKAGE="microsoft.kernelmemory.postgres"

# Move to the root of the repository
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${ROOT}"

# Clean up the build
echo "---- Cleaning up the build"
rm -f ${PROJECT}/bin/Release/*.nupkg
rm -f ${PROJECT}/bin/Release/*.snupkg

# Build only the project
echo "---- Building the project"
cd ${PROJECT}
dotnet build --no-cache --force -c Release

# Check if the nupkg has been created
echo "---- Checking if the nupkg has been created"
if [ ! -f bin/Release/*.nupkg ]; then
    echo "ERROR: the nupkg file has not been created"
    exit 1
fi

# Clear the cache
echo "---- Clearing the cache"
if [ -d "${HOME}/.nuget/packages/${PACKAGE}" ]; then
    echo "Cache content before purge:"
    ls -la "${HOME}/.nuget/packages/${PACKAGE}"
    rm -fR "${HOME}/.nuget/packages/${PACKAGE}"
    if [ -d "${HOME}/.nuget/packages/${PACKAGE}" ]; then
        echo "ERROR: unable to clear cache at ${HOME}/.nuget/packages/${PACKAGE}"
        exit 1
    fi
fi

# Copy the packages to the packages folder
echo "---- Copying the packages to the packages folder"
mv bin/Release/*.nupkg ${ROOT}/packages/
mv bin/Release/*.snupkg ${ROOT}/packages/

# Repo clean up
echo "---- Cleaning up the repository builds"
cd "${ROOT}"
rm -fR TestApplication/bin TestApplication/obj
dotnet clean --nologo -v m -c Debug
dotnet clean --nologo -v m -c Release
