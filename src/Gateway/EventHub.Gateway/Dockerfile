FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY src/Gateway/ src/Gateway/

RUN dotnet publish src/Gateway/EventHub.Gateway/EventHub.Gateway.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "EventHub.Gateway.dll"]
