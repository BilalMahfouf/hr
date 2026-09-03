# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Api/PublicApi/PublicApi.csproj", "src/Api/PublicApi/"]
RUN for i in 1 2 3; do dotnet restore "./src/Api/PublicApi/PublicApi.csproj" --verbosity normal /p:NuGetPackageDownloadTimeout=300 && break || sleep 10; done
COPY . .
WORKDIR "/src/src/Api/PublicApi"
RUN dotnet build "./PublicApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PublicApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production - use Google Chrome base image with .NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install Chrome and dependencies in a single layer with retries
USER root
RUN for i in 1 2 3; do \
    apt-get update && apt-get install -y \
    wget gnupg ca-certificates \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --dearmor -o /usr/share/keyrings/google-chrome.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-chrome.gpg] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/* \
    && break || sleep 10; done

# Create a writable directory for Puppeteer browser cache
RUN mkdir -p /home/app/.cache/puppeteer && chown -R $APP_UID:$APP_UID /home/app

# Switch back to non-root user
USER $APP_UID

# Tell Puppeteer where to store its cache
ENV PUPPETEER_CACHE_DIR=/home/app/.cache/puppeteer

# Use the system-installed Chrome - skip runtime download entirely
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PublicApi.dll"]