FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8099

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["services/binah-webhooks/Binah.Webhooks.csproj", "services/binah-webhooks/"]
COPY ["libraries/binah-core/Binah.Core.csproj", "libraries/binah-core/"]
COPY ["libraries/binah-contracts/Binah.Contracts.csproj", "libraries/binah-contracts/"]

# Restore dependencies
RUN dotnet restore "services/binah-webhooks/Binah.Webhooks.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/services/binah-webhooks"
RUN dotnet build "Binah.Webhooks.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Binah.Webhooks.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.Webhooks.dll"]
