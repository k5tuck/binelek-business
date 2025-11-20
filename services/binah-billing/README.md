# Binah Billing Service

> **📚 For detailed technical documentation, see [docs/services/binah-billing.md](../../docs/services/binah-billing.md)**

Stripe-powered subscription management and billing service for the Binelek Platform.

## Features

- **Subscription Management**: Create, update, and cancel subscriptions
- **Stripe Integration**: Full Stripe API integration for payments
- **Quota Enforcement**: Track and enforce usage limits by plan
- **Webhook Handling**: Process Stripe webhook events automatically
- **Multi-Tenant Support**: Tenant-isolated billing and subscriptions
- **Usage Tracking**: Monitor API calls, users, and properties

## Tech Stack

- .NET 8
- Stripe.NET SDK (v45.0)
- PostgreSQL (via Entity Framework Core)
- JWT Authentication
- Serilog for logging

## Getting Started

### Prerequisites

1. Stripe account (get keys from https://dashboard.stripe.com/test/apikeys)
2. PostgreSQL database
3. .NET 8 SDK

### Environment Variables

```bash
# Database
ConnectionStrings__PostgreSQL=Host=postgres;Port=5432;Database=Binah_billing;Username=Binah;Password=***

# JWT Auth (must match auth service)
Jwt__Secret=your-jwt-secret
Jwt__Issuer=binah-auth
Jwt__Audience=binah-clients

# Stripe
Stripe__SecretKey=sk_test_***
Stripe__PublishableKey=pk_test_***
Stripe__WebhookSecret=whsec_***

# CORS
Cors__AllowedOrigins=http://localhost:5173,http://localhost:3000
```

### Database Setup

1. Run the migration script:
```bash
psql -U Binah -d Binah_billing -f migrations/001_initial_schema.sql
```

2. Update tenant table (in auth database):
```bash
psql -U Binah -d Binah_auth -f ../../scripts/postgres-add-stripe-columns.sql
```

### Running Locally

```bash
cd services/binah-billing
dotnet restore
dotnet run
```

The service will be available at http://localhost:8095

### Running with Docker

```bash
docker-compose up billing
```

## API Endpoints

### Subscriptions

- `POST /api/billing/subscriptions` - Create subscription
- `GET /api/billing/subscriptions/{id}` - Get subscription
- `GET /api/billing/subscriptions/tenant/{tenantId}/active` - Get active subscription
- `GET /api/billing/subscriptions/tenant/{tenantId}` - Get subscription history
- `PUT /api/billing/subscriptions/{id}` - Update subscription
- `POST /api/billing/subscriptions/{id}/cancel` - Cancel subscription

### Quota Management

- `GET /api/billing/subscriptions/tenant/{tenantId}/quota` - Get current quota
- `POST /api/billing/subscriptions/tenant/{tenantId}/quota/check` - Check quota
- `POST /api/billing/subscriptions/tenant/{tenantId}/usage` - Record usage

### Webhooks

- `POST /api/billing/webhooks/stripe` - Stripe webhook handler

## Pricing Plans

### Basic Plan ($29/month)
- 5 users
- 100 properties
- 10,000 API calls/month
- Basic features

### Pro Plan ($99/month)
- 25 users
- 1,000 properties
- 100,000 API calls/month
- Advanced features + AI

### Enterprise Plan ($499/month)
- Unlimited users
- Unlimited properties
- Unlimited API calls
- All features + SSO

## Stripe Setup

### 1. Create Products in Stripe Dashboard

1. Go to https://dashboard.stripe.com/test/products
2. Create three products: Basic, Pro, Enterprise
3. Add recurring prices for each (monthly and yearly)
4. Copy the Price IDs

### 2. Update Pricing Configuration

Edit `Models/PricingPlan.cs` and update the Stripe Price IDs:

```csharp
StripePriceId = "price_xxxxx", // Your actual Stripe Price ID
StripeProductId = "prod_xxxxx", // Your actual Stripe Product ID
```

### 3. Configure Webhooks

1. Go to https://dashboard.stripe.com/test/webhooks
2. Add endpoint: `https://your-domain.com/api/billing/webhooks/stripe`
3. Select events to listen for:
   - customer.created
   - customer.updated
   - customer.deleted
   - customer.subscription.created
   - customer.subscription.updated
   - customer.subscription.deleted
   - customer.subscription.trial_will_end
   - invoice.created
   - invoice.payment_succeeded
   - invoice.payment_failed
   - payment_method.attached
   - payment_method.detached
4. Copy the webhook signing secret to `STRIPE_WEBHOOK_SECRET`

## Usage Examples

### Create Subscription

```bash
curl -X POST http://localhost:8095/api/billing/subscriptions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "uuid",
    "email": "customer@example.com",
    "planName": "pro",
    "startTrial": true,
    "trialDays": 14
  }'
```

### Check Quota

```bash
curl -X POST http://localhost:8095/api/billing/subscriptions/tenant/{tenantId}/quota/check?quotaType=users&amount=1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Record Usage

```bash
curl -X POST http://localhost:8095/api/billing/subscriptions/tenant/{tenantId}/usage?usageType=api_calls&quantity=1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Frontend Integration

The billing UI components are available in the frontend:

```tsx
import { SubscriptionManager } from './components/billing/SubscriptionManager';

<SubscriptionManager
  tenantId={currentTenantId}
  onSubscriptionChange={(sub) => console.log('Subscription updated', sub)}
/>
```

Components included:
- `SubscriptionManager` - Full subscription management UI
- `PricingPlans` - Interactive pricing cards
- `SubscriptionCard` - Current plan display
- `QuotaDisplay` - Usage meters and limits

## Architecture

```
Frontend (React)
    ↓
API Gateway (binah-api)
    ↓
Billing Service (binah-billing)
    ↓
    ├── Stripe API (payments)
    └── PostgreSQL (billing data)
```

## Database Schema

- **customers** - Stripe customer mapping
- **subscriptions** - Subscription details
- **invoices** - Invoice history
- **payment_methods** - Saved payment methods
- **usage_records** - Usage tracking

## Security

- JWT authentication required for all endpoints (except webhooks)
- Webhook signature verification
- Tenant isolation via tenant_id
- CORS protection
- Rate limiting (via API Gateway)

## Monitoring

- Health check: http://localhost:8095/health
- Logs: `logs/billing-{date}.log`
- Serilog structured logging

## Testing

Run unit tests:
```bash
dotnet test
```

## Production Deployment

1. Update Stripe keys to live mode
2. Configure webhook endpoint for production domain
3. Update CORS origins
4. Enable HTTPS
5. Configure production database
6. Set appropriate session timeout
7. Review and update quota limits

## Support

For issues or questions, contact the platform team or refer to the main README.
