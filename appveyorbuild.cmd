@echo off

dotnet restore -v Minimal
cd DarkStatsCore 
setlocal
call npm install
node copypackages.js
dotnet publish -r win10-x64 -o .\darkstatscore-win10-x64
dotnet publish -r osx.10.12-x64 -o .\darkstatscore-osx.10.12-x64
dotnet publish -r debian.8-x64 -o .\darkstatscore-debian.8-x64
echo Win %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME% >darkstatscore-win10-x64\BUILD_VERSION
echo Deb %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME% >darkstatscore-debian.8-x64\BUILD_VERSION
echo OSX %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME% >darkstatscore-osx.10.12-x64\BUILD_VERSION
