FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY OficinaDigital.sln ./
COPY src/OficinaDigital.Domain/OficinaDigital.Domain.csproj                   src/OficinaDigital.Domain/
COPY src/OficinaDigital.Application/OficinaDigital.Application.csproj         src/OficinaDigital.Application/
COPY src/OficinaDigital.Infrastructure/OficinaDigital.Infrastructure.csproj   src/OficinaDigital.Infrastructure/
COPY src/OficinaDigital.Api/OficinaDigital.Api.csproj                         src/OficinaDigital.Api/

RUN dotnet restore src/OficinaDigital.Api/OficinaDigital.Api.csproj

COPY src/ src/

RUN dotnet publish src/OficinaDigital.Api/OficinaDigital.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=America/Sao_Paulo

COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["dotnet", "OficinaDigital.Api.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "OficinaDigital.Api.dll"]
