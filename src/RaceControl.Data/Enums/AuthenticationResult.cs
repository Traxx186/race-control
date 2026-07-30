namespace RaceControl.Data.Enums;

public enum AuthenticationResult
{
    Success,
    NoToken,
    InvalidToken,
    InvalidSubscriptionStatus,
    ExpiredToken,
}