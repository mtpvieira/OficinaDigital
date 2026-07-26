FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OficinaDigital.Api/OficinaDigital.Api.csproj", "OficinaDigital.Api/"]
RUN dotnet restore "OficinaDigital.Api/OficinaDigital.Api.csproj"
COPY . .
WORKDIR "/src/OficinaDigital.Api"
RUN dotnet build "./OficinaDigital.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OficinaDigital.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OficinaDigital.Api.dll"]
