# Build context: repository root
#   docker build -f GameBackend.api/Dockerfile -t gamebackend-api .

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer is cached until dependencies change
COPY GameBackend.Domain/GameBackend.Domain.csproj GameBackend.Domain/
COPY GameBackend.Application/GameBackend.Application.csproj GameBackend.Application/
COPY GameBackend.Infrastructure/GameBackend.Infrastructure.csproj GameBackend.Infrastructure/
COPY GameBackend.api/GameBackend.api.csproj GameBackend.api/
RUN dotnet restore GameBackend.api/GameBackend.api.csproj

COPY GameBackend.Domain/ GameBackend.Domain/
COPY GameBackend.Application/ GameBackend.Application/
COPY GameBackend.Infrastructure/ GameBackend.Infrastructure/
COPY GameBackend.api/ GameBackend.api/
RUN dotnet publish GameBackend.api/GameBackend.api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Non-root user provided by the official .NET images
USER $APP_UID
# Plain "Urls" (not ASPNETCORE_URLS): appsettings.json sets "Urls" too, and unprefixed
# environment variables are applied after appsettings.json, so this value wins in the container
ENV Urls=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GameBackend.api.dll"]
