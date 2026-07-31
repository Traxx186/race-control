using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;

namespace RaceControl.Services;

public interface IF1AuthService
{
    string? AccessToken { get; }

    TokenPayloadDto? Payload { get; }

    AuthenticationResult IsAuthenticated { get; }

    void Refresh(string? token);
}