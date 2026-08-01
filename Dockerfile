# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copia só o csproj primeiro para a camada de restore ficar em cache
# enquanto o código-fonte muda.
COPY src/MyCEP.API/MyCEP.API.csproj src/MyCEP.API/
RUN dotnet restore src/MyCEP.API/MyCEP.API.csproj

COPY src/ src/
RUN dotnet publish src/MyCEP.API/MyCEP.API.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# ICU: sem isso o .NET cai em modo invariant e a formatação pt-BR
# (datas, acentuação em ordenação) fica errada.
#
# A imagem oficial do .NET 8 já traz um usuário não-root chamado "app";
# tentar recriá-lo falha com "group in use".
RUN apk add --no-cache icu-libs tzdata

COPY --from=build --chown=app:app /app/publish .

USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=America/Sao_Paulo

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "MyCEP.API.dll"]
