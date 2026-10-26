# Build context is the REPO ROOT (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/EverRecall.Api/EverRecall.Api.csproj src/EverRecall.Api/
RUN dotnet restore src/EverRecall.Api/EverRecall.Api.csproj

COPY src/ src/
RUN dotnet publish src/EverRecall.Api/EverRecall.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is only here for the docker-compose healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# SQLite lives here; docker-compose mounts a volume on /app/data.
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/everrecall.db"

COPY --from=build /app/publish .

RUN mkdir -p /app/data && chown -R app:app /app
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "EverRecall.Api.dll"]
