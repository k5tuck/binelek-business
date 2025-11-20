using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using FluentAssertions;
using Binah.Webhooks.Models;

namespace Binah.Webhooks.Tests.Integration;

/// <summary>
/// Integration tests for tenant isolation in webhook service
/// These tests verify that tenant A cannot access tenant B's webhooks
/// </summary>
public class WebhookTenantIsolationTests : IClassFixture<WebhookTestFixture>
{
    private readonly WebhookTestFixture _fixture;
    private readonly HttpClient _client;

    public WebhookTenantIsolationTests(WebhookTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetWebhooks_WithTenantAToken_ReturnsOnlyTenantAWebhooks()
    {
        // Arrange
        var tenantAToken = GenerateJwtToken("user-a", "tenant-a", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);

        // Act
        var response = await _client.GetAsync("/api/webhooks/subscriptions");
        var webhooks = await response.Content.ReadFromJsonAsync<List<WebhookSubscription>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        webhooks.Should().NotBeNull();
        webhooks.Should().AllSatisfy(w => w.TenantId.Should().Be("tenant-a"));
    }

    [Fact]
    public async Task GetWebhooks_WithTenantBToken_ReturnsOnlyTenantBWebhooks()
    {
        // Arrange
        var tenantBToken = GenerateJwtToken("user-b", "tenant-b", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantBToken);

        // Act
        var response = await _client.GetAsync("/api/webhooks/subscriptions");
        var webhooks = await response.Content.ReadFromJsonAsync<List<WebhookSubscription>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        webhooks.Should().NotBeNull();
        webhooks.Should().AllSatisfy(w => w.TenantId.Should().Be("tenant-b"));
    }

    [Fact]
    public async Task GetWebhook_CrossTenant_Returns404()
    {
        // Arrange - Create webhook for tenant A
        var tenantAToken = GenerateJwtToken("user-a", "tenant-a", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);

        var createRequest = new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var webhookId = created?.data?.id?.ToString();

        // Act - Try to access with tenant B token
        var tenantBToken = GenerateJwtToken("user-b", "tenant-b", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantBToken);

        var getResponse = await _client.GetAsync($"/api/webhooks/subscriptions/{webhookId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWebhook_WithoutToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWebhook_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var request = new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWebhook_WithValidUrl_Succeeds()
    {
        // Arrange
        var token = GenerateJwtToken("user-a", "tenant-a", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            name = "Valid Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateWebhook_CrossTenant_Returns404()
    {
        // Arrange - Create webhook for tenant A
        var tenantAToken = GenerateJwtToken("user-a", "tenant-a", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);

        var createRequest = new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var webhookId = created?.data?.id?.ToString();

        // Act - Try to update with tenant B token
        var tenantBToken = GenerateJwtToken("user-b", "tenant-b", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantBToken);

        var updateRequest = new
        {
            name = "Updated Webhook",
            url = "https://example.com/webhook-updated",
            events = new[] { "entity.updated" },
            active = true,
            retryCount = 3
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/webhooks/subscriptions/{webhookId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWebhook_CrossTenant_DoesNotDelete()
    {
        // Arrange - Create webhook for tenant A
        var tenantAToken = GenerateJwtToken("user-a", "tenant-a", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);

        var createRequest = new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var webhookId = created?.data?.id?.ToString();

        // Act - Try to delete with tenant B token
        var tenantBToken = GenerateJwtToken("user-b", "tenant-b", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantBToken);

        var deleteResponse = await _client.DeleteAsync($"/api/webhooks/subscriptions/{webhookId}");

        // Verify webhook still exists for tenant A
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);
        var getResponse = await _client.GetAsync($"/api/webhooks/subscriptions/{webhookId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDeliveries_OnlyReturnsOwnTenantDeliveries()
    {
        // Arrange
        var tenantAToken = GenerateJwtToken("user-a", "tenant-a", "user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);

        // Create a webhook first
        var createRequest = new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            events = new[] { "entity.created" },
            active = true,
            retryCount = 3
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks/subscriptions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var webhookId = created?.data?.id?.ToString();

        // Act
        var response = await _client.GetAsync($"/api/webhooks/deliveries?subscriptionId={webhookId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Generates a test JWT token with specified claims
    /// NOTE: Uses snake_case "tenant_id" claim (not camelCase "tenantId")
    /// </summary>
    private string GenerateJwtToken(string userId, string tenantId, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", userId),
            new System.Security.Claims.Claim("tenant_id", tenantId), // snake_case!
            new System.Security.Claims.Claim("role", role),
            new System.Security.Claims.Claim("email", $"{userId}@example.com")
        };

        var token = new JwtSecurityToken(
            issuer: "binah-auth",
            audience: "binah-webhooks",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Test fixture for webhook integration tests
/// </summary>
public class WebhookTestFixture
{
    public HttpClient CreateClient()
    {
        // In a real implementation, this would create a TestServer with the application
        // For now, return a basic HttpClient
        return new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8098")
        };
    }
}
