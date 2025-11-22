FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 8095

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
COPY ["services/binah-billing/Binah.Billing.csproj", "services/binah-billing/"]
RUN dotnet restore "services/binah-billing/Binah.Billing.csproj"

# Copy source files
COPY services/binah-billing/ services/binah-billing/

# Build
WORKDIR "/src/services/binah-billing"
RUN dotnet build "Binah.Billing.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Binah.Billing.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.Billing.dll"]
