namespace RaceControl.Data.Dtos;

/// <summary>
/// Body of the subscriptionToken
/// </summary>
/// <param name="SubscriptionStatus">What the status of the account subscription is.</param>
/// <param name="SubscribedProduct">Which products the user is subscribed to.</param>
/// <param name="Exp">When the token will expire.</param>
/// <param name="Iat">When the token was issued.</param>
public sealed record TokenPayloadDto(
    string SubscriptionStatus,
    string? SubscribedProduct,
    int Exp,
    int Iat
)
{
    public DateTimeOffset Expiry => DateTimeOffset.FromUnixTimeSeconds(Exp);

    public DateTimeOffset IssuedAt => DateTimeOffset.FromUnixTimeSeconds(Iat);
}