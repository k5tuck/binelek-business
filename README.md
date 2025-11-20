# Binelek Business Services

Business-focused microservices for the Binelek platform, extracted from the monorepo.

## Services

### binah-billing (Port 8095)
**Technology:** .NET 8.0  
**Database:** PostgreSQL  
**Purpose:** Subscription management and billing via Stripe

**Features:**
- Stripe integration for payments
- Subscription lifecycle management
- Invoice generation
- Payment webhooks
- Hangfire background jobs for recurring billing

### binah-webhooks (Port 8098)
**Technology:** .NET 8.0  
**Database:** PostgreSQL  
**Purpose:** Webhook delivery and GitHub integration

**Features:**
- HTTP webhook delivery with retries
- GitHub Octokit integration
- Autonomous PR creation
- Rate limiting (AspNetCoreRateLimit)
- Polly resilience patterns

## Architecture

**NuGet Packages:** Uses v1.0.0-beta.1 of:
- Binah.Core (exceptions, middleware, utilities)
- Binah.Contracts (DTOs, events, interfaces)
- Binah.Infrastructure (DbContexts, repositories, Kafka)

**Package Feed:** GitHub Packages (`https://nuget.pkg.github.com/k5tuck/index.json`)

## Building

```bash
# Restore packages
dotnet restore Binelek.Business.sln

# Build all services
dotnet build Binelek.Business.sln --configuration Release

# Build specific service
dotnet build services/binah-billing/Binah.Billing.csproj
```

## Running Locally

```bash
# Run binah-billing
cd services/binah-billing
dotnet run

# Run binah-webhooks
cd services/binah-webhooks
dotnet run
```

## CI/CD

GitHub Actions workflow builds both services on push to `main` or `develop` branches.

**Jobs:**
- `build-and-test` - Builds binah-billing and binah-webhooks
- `security-scan` - Trivy security scanning with SARIF upload

## Related Repositories

- [binelek-shared](https://github.com/k5tuck/binelek-shared) - Shared libraries (NuGet packages)
- [binelek-core](https://github.com/k5tuck/binelek-core) - Core platform services
- [binelek-data](https://github.com/k5tuck/binelek-data) - Data processing services

## License

Proprietary - All Rights Reserved
