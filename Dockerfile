FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# https://hub.docker.com/_/microsoft-dotnet
ARG NUGET_USER
ARG NUGET_PASS

RUN dotnet nuget add source --username ${NUGET_USER} --password ${NUGET_PASS} --store-password-in-clear-text https://nuget.pkg.github.com/LightBlueFox-Labs/index.json --name="GH: LightBlueFox-Industries"



WORKDIR /src/LightBlueFox.Games.Poker
COPY LightBlueFox.Games.Poker/*.csproj .
COPY LightBlueFox.Games.Poker/. .

WORKDIR /src/LightBlueFox.Games.Poker.Web
COPY LightBlueFox.Games.Poker.Web/*.csproj .
COPY LightBlueFox.Games.Poker.Web/. .

RUN dotnet publish LightBlueFox.Games.Poker.Web.csproj -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0.0
EXPOSE 80
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./LightBlueFox.Games.Poker.Web"]
