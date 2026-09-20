FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY EventHub.slnx ./
COPY src/EventHub.Shared.Authentication/ src/EventHub.Shared.Authentication/
COPY src/EventHub.Shared.Contracts/ src/EventHub.Shared.Contracts/
COPY src/EventHub.Shared.Messaging/ src/EventHub.Shared.Messaging/
COPY src/Events/ src/Events/

RUN dotnet publish src/Events/EventHub.Events.Api/EventHub.Events.Api.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "EventHub.Events.Api.dll"]
