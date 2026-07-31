using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;
using RaceControl.Options;

namespace RaceControl.Services;

public sealed class F1AuthService : IF1AuthService
{
    private readonly IOptionsMonitor<RaceControlOptions> _optionsMonitor;
    private readonly ILogger<F1AuthService> _logger;

    public string? AccessToken { get; private set; }
    public TokenPayloadDto? Payload { get; private set; }
    public AuthenticationResult IsAuthenticated { get; private set; }

    public F1AuthService(
        IOptionsMonitor<RaceControlOptions> optionsMonitor,
        ILogger<F1AuthService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        optionsMonitor.OnChange(options => Refresh(options.Formula1AccessToken));
        Refresh(_optionsMonitor.CurrentValue.Formula1AccessToken);
    }

    public void Refresh(string? token)
    {
        var result = CheckToken(token, out var payload);
        Payload = payload;
        IsAuthenticated = result;
        AccessToken = result == AuthenticationResult.Success
            ? GetSubscriptionTokenFromAccessToken(token!)
            : null;
    }

    private AuthenticationResult CheckToken(string? accessToken, out TokenPayloadDto? token)
    {
        token = null;
        if (string.IsNullOrWhiteSpace(accessToken))
            return AuthenticationResult.NoToken;

        try
        {
            token = GetTokenPayloadFromAccessToken(accessToken);
            if (token is null)
                return AuthenticationResult.InvalidToken;

            if (!token.SubscriptionStatus.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "[F1AuthService] Access token doesn't have an active subscription ({SubscriptionStatus}). It cannot be used for live timing data",
                    token.SubscriptionStatus
                );
                return AuthenticationResult.InvalidSubscriptionStatus;
            }

            if (token.Expiry >= DateTimeOffset.UtcNow)
                return AuthenticationResult.Success;

            _logger.LogError("[F1AuthService] Access token expired, login again");
            return AuthenticationResult.ExpiredToken;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "[F1AuthService] Failed to read token");
            return AuthenticationResult.InvalidToken;
        }
    }

    /// <summary>
    /// Gets the token payload data from the given access token
    /// </summary>
    /// <param name="accessToken">Token to be used.</param>
    /// <returns>The token payload.</returns>
    private TokenPayloadDto? GetTokenPayloadFromAccessToken(string accessToken)
    {
        var subscriptionToken = GetSubscriptionTokenFromAccessToken(accessToken)!;

        // The token is split into three parts, a header, body, and sig. We only want to read the body.
        var tokenPart = subscriptionToken.Split('.')[1];

        // For some reason, the base64 encoded string sometimes doesn't have enough padding chars at the end
        // Base64 strings should be a multiple of 4
        var missingPaddingChars = tokenPart.Length % 4;
        if (missingPaddingChars > 0)
        {
            tokenPart += new string('=', 4 - missingPaddingChars);
        }

        var tokenPayload = JsonSerializer.Deserialize<TokenPayloadDto>(Convert.FromBase64String(tokenPart));
        return tokenPayload;
    }

    /// <summary>
    /// Get the value from the subscriptionToken property in the access token.
    /// </summary>
    /// <param name="accessToken">The access token to search in.</param>
    /// <returns>The stored subscriptionToken.</returns>
    private string? GetSubscriptionTokenFromAccessToken(string accessToken)
    {
        var jsonString = Uri.UnescapeDataString(accessToken);

        return JsonNode.Parse(jsonString)?["data"]?["subscriptionToken"]?.GetValue<string>();
    }
}
