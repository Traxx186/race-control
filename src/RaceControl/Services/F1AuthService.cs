using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace RaceControl.Services;

public class F1AuthService(
    ILogger<F1AuthService> logger,
    IWebHostEnvironment environment,
    IHttpClientFactory httpClientFactory)
{
    private const string JwksUrl = "https://api.formula1.com/static/jwks.json";
    public string AuthDataFile => Path.Combine(environment.ContentRootPath, "storage", "f1auth.json");

    /// <summary>
    /// Stores the given token in the f1auth.json file
    /// </summary>
    /// <param name="token">Token to be stored.</param>
    public async Task StoreAuthToken(string token)
    {
        await using var stream = File.Exists(AuthDataFile)
            ? File.OpenWrite(AuthDataFile)
            : File.Create(AuthDataFile);

        var authData = new AuthData(token);
        var json = JsonSerializer.Serialize(authData);

        await using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync(json);
    }

    /// <summary>
    /// Gets the stored subscription token.
    /// </summary>
    /// <returns>Stored subscription token.</returns>
    public async Task<string> GetAuthToken()
    {
        var authFileContent = await File.OpenText(AuthDataFile).ReadToEndAsync();
        var authData = JsonSerializer.Deserialize<AuthData>(authFileContent);

        return authData!.SubscriptionToken;
    }

    /// <summary>
    /// Checks if the given token is valid.
    /// </summary>
    /// <param name="token">Token to be validated.</param>
    /// <returns>If the token is valid.</returns>
    public async Task<ClaimsPrincipal> ValidateJwt(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token));

        var jwks = await FetchF1Jwks();
        var jwk = jwks.Keys.First();
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            IssuerSigningKey = jwk,
            ValidateLifetime = true,
            ValidateAudience = false,
            ValidateIssuer = false,
        };

        return tokenHandler.ValidateToken(token, validationParams, out _);;
    }

    /// <summary>
    /// Gets the JWKS file from the F1 API.
    /// </summary>
    /// <returns>JWKS File content.</returns>
    private async Task<JsonWebKeySet> FetchF1Jwks()
    {
        using var client = httpClientFactory.CreateClient();
        using var response = await client.GetAsync(JwksUrl);
        var responseContent = await response.Content.ReadAsStringAsync();

        return new JsonWebKeySet(responseContent);
    }

    /// <summary>
    /// Structure of the f1auth.json file.
    /// </summary>
    /// <param name="SubscriptionToken">The API subscription token.</param>
    public sealed record AuthData(
        string SubscriptionToken
    );
}
