FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8099

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG GITHUB_TOKEN
WORKDIR /src

# Configure GitHub Packages as NuGet source
RUN if [ -n "$GITHUB_TOKEN" ]; then \
      dotnet nuget add source https://nuget.pkg.github.com/k5tuck/index.json \
        --name github \
        --username k5tuck \
        --password "$GITHUB_TOKEN" \
        --store-password-in-clear-text; \
    fi

# Copy service project
COPY ["services/binah-webhooks/Binah.Webhooks.csproj", "services/binah-webhooks/"]
RUN dotnet restore "services/binah-webhooks/Binah.Webhooks.csproj"

# Copy source files
COPY services/binah-webhooks/ services/binah-webhooks/

# Build
WORKDIR "/src/services/binah-webhooks"
RUN dotnet build "Binah.Webhooks.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Binah.Webhooks.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.Webhooks.dll"]
