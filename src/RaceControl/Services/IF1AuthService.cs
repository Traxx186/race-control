namespace RaceControl.Services;

public interface IF1AuthService
{
    string? AccessToken { get; }

    F1AuthService.TokenPayload? Payload { get; }

    F1AuthService.AuthenticationResult IsAuthenticated { get; }

    void Refresh(string? token);
}