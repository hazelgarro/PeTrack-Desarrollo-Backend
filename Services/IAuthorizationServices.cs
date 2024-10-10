using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using System.Security.Claims;

namespace APIPetrack.Services
{
    public interface IAuthorizationServices
    {
        Task<AuthorizationResponse> LoginUserAsync(AppUser.LoginUser loginPetOwner);
        ClaimsPrincipal ValidateToken(string token);
    }
}
