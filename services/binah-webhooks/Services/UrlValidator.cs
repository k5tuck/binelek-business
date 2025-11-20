using System.Net;
using System.Net.Sockets;

namespace Binah.Webhooks.Services;

/// <summary>
/// URL validation service with SSRF protection
/// Prevents webhooks from targeting internal/private IP addresses
/// </summary>
public class UrlValidator
{
    private readonly ILogger<UrlValidator> _logger;

    // Blocked IP ranges for SSRF protection
    private readonly List<string> _blockedHostnames = new()
    {
        "localhost",
        "0.0.0.0",
        "127.0.0.1",
        "::1", // IPv6 loopback
        "169.254.169.254", // AWS metadata service
        "metadata.google.internal" // GCP metadata service
    };

    public UrlValidator(ILogger<UrlValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a webhook URL for SSRF vulnerabilities
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>True if the URL is safe to use, false otherwise</returns>
    public bool IsUrlSafe(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("Empty URL provided for validation");
            return false;
        }

        // Parse URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Invalid URL format: {Url}", url);
            return false;
        }

        // Only allow HTTP/HTTPS
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            _logger.LogWarning("Blocked non-HTTP(S) scheme: {Scheme} in URL: {Url}", uri.Scheme, url);
            return false;
        }

        // Check against blocked hostnames
        if (_blockedHostnames.Contains(uri.Host.ToLower()))
        {
            _logger.LogWarning("Blocked URL with forbidden hostname: {Host}", uri.Host);
            return false;
        }

        // Resolve hostname to IP addresses
        try
        {
            var hostEntry = Dns.GetHostEntry(uri.Host);

            foreach (var ip in hostEntry.AddressList)
            {
                if (IsPrivateOrInternalIp(ip))
                {
                    _logger.LogWarning("SSRF attempt blocked: URL {Url} resolves to private IP {IP}", url, ip);
                    return false;
                }
            }
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Could not resolve hostname: {Host}", uri.Host);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hostname resolution for: {Host}", uri.Host);
            return false;
        }

        _logger.LogDebug("URL validated successfully: {Url}", url);
        return true;
    }

    /// <summary>
    /// Checks if an IP address is in a private or internal range
    /// </summary>
    private bool IsPrivateOrInternalIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        // IPv4 checks
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            // Loopback: 127.0.0.0/8
            if (bytes[0] == 127)
                return true;

            // Private: 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // Private: 172.16.0.0/12 (172.16.0.0 - 172.31.255.255)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // Private: 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // Link-local: 169.254.0.0/16
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // Broadcast: 255.255.255.255
            if (bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255)
                return true;

            // 0.0.0.0
            if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
                return true;
        }

        // IPv6 checks
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Loopback: ::1
            if (IPAddress.IsLoopback(ip))
                return true;

            // Link-local: fe80::/10
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                return true;

            // Unique local: fc00::/7
            if ((bytes[0] & 0xfe) == 0xfc)
                return true;

            // Site-local (deprecated): fec0::/10
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0xc0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a validation error message for a URL
    /// </summary>
    public string GetValidationError(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "URL cannot be empty";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "Invalid URL format";

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return "Only HTTP and HTTPS protocols are allowed";

        if (_blockedHostnames.Contains(uri.Host.ToLower()))
            return "URL points to a blocked hostname";

        try
        {
            var hostEntry = Dns.GetHostEntry(uri.Host);

            foreach (var ip in hostEntry.AddressList)
            {
                if (IsPrivateOrInternalIp(ip))
                {
                    return "URL resolves to a private or internal IP address";
                }
            }
        }
        catch (SocketException)
        {
            return "Could not resolve hostname";
        }
        catch (Exception)
        {
            return "Error validating URL";
        }

        return string.Empty; // No error
    }
}
