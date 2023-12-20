dotnet publish RymRss --configuration Release --output bin\publish
dotnet ef migrations bundle --project RymRss --configuration Release --self-contained --target-runtime win-x64 --output bin\sqlitebundle.exe --force
dotnet build Setup --configuration Release -p:Platform=x64 --no-dependencies --output bin\setup
@echo Build finished at %DATE% %TIME%
