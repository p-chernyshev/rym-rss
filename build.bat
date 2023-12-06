dotnet ef migrations bundle --configuration Release --self-contained -r win-x64 -o bin\sqlitebundle.exe --force
dotnet publish RymRss.csproj -c Release -o bin\publish
dotnet build Setup -c Release -p:Platform=x64 --no-dependencies -o Setup\bin
@echo Build finished at %DATE% %TIME%
