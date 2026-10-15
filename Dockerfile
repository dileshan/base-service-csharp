# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until the csproj changes.
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj

COPY Api/ Api/
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Non-root user provided by the official .NET images.
USER $APP_UID

ENTRYPOINT ["dotnet", "Api.dll"]
