# SupplyCollectorDataLoader
Base class for data loaders

## Building
Run `dotnet build`

## Publishing to MyGet feed
- Generate API key if necessary at `https://www.myget.org/profile/Me#!/AccessTokens`
- Build library and run 
`dotnet publish && dotnet nuget push SupplyCollectorDataLoader/bin/Debug/*.nupkg -k $NUGET_KEY -s https://www.myget.org/F/s2/api/v2/package`
Replace $NUGET_KEY with your api key
