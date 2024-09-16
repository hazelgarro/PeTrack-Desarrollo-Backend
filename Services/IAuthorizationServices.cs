using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;

namespace APIPetrack.Services
{
    public interface IAuthorizationServices
    {
        Task<AuthorizationResponse> LoginUserAsync(AppUser.LoginUser loginPetOwner);
    }
}
