FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.S88Pay/Tgs.Payment.S88Pay.csproj", "Tgs.Payment.S88Pay/"]
COPY ["S88Pay.Core/S88Pay.Core.csproj", "S88Pay.Core/"]
COPY ["S88Pay.Client/S88Pay.Client.csproj", "S88Pay.Client/"]
RUN dotnet restore "./Tgs.Payment.S88Pay/Tgs.Payment.S88Pay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.S88Pay"
RUN dotnet build "./Tgs.Payment.S88Pay.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.S88Pay.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.S88Pay.dll"]