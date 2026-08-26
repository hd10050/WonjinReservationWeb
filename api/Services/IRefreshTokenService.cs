using WonjinApi.Models;

namespace WonjinApi.Services;

public interface IRefreshTokenService
{
    Task<(RefreshToken Entity, string RawToken)> CreateAsync(int userId);
    Task<RefreshToken?> ValidateAsync(string rawToken);
    Task RevokeAsync(string rawToken);
    Task RevokeAllForUserAsync(int userId);
}
