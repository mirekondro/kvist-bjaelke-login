# ---------- Stage 1: build (SDK image, used only for compiling) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the .csproj first and restore -> Docker caches the NuGet packages
COPY src/LoginPlatform/LoginPlatform.csproj src/LoginPlatform/
RUN dotnet restore src/LoginPlatform/LoginPlatform.csproj

# Then copy the rest of the source code and publish
COPY src/LoginPlatform/ src/LoginPlatform/
RUN dotnet publish src/LoginPlatform/LoginPlatform.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: runtime (smaller image without SDK and source code) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Folder for the SQLite database and keys - owned by the unprivileged "app" user
RUN mkdir -p /app/data && chown $APP_UID /app/data

COPY --from=build /app/publish .

# The application does not run as root
USER $APP_UID

# Kestrel listens on 0.0.0.0:8080 (all interfaces, not just localhost)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LoginPlatform.dll"]
