FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 8095

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy library projects
COPY ["libraries/binah-contracts/Binah.Contracts.csproj", "libraries/binah-contracts/"]
COPY ["libraries/binah-core/Binah.Core.csproj", "libraries/binah-core/"]
COPY ["libraries/binah-infrastructure/Binah.Infrastructure.csproj", "libraries/binah-infrastructure/"]

# Copy service project
COPY ["services/binah-billing/Binah.Billing.csproj", "services/binah-billing/"]

# Restore dependencies
RUN dotnet restore "services/binah-billing/Binah.Billing.csproj"

# Copy all source files
COPY libraries/ libraries/
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
