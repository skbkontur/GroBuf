dotnet build --force --no-incremental --configuration Release ./GroBuf.sln
dotnet pack --no-build --configuration Release ./GroBuf.sln
cd ./GroBuf/bin/Release
dotnet nuget push *.nupkg -s https://nuget.org
pause