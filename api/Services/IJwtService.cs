using WonjinApi.Models;

namespace WonjinApi.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
}
