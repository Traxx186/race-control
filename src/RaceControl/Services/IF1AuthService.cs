using RaceControl.Data.Enums;

namespace RaceControl.Services;

public interface IF1AuthService
{
    string? AccessToken { get; }

    F1AuthService.TokenPayload? Payload { get; }

    AuthenticationResult IsAuthenticated { get; }

    void Refresh(string? token);
}