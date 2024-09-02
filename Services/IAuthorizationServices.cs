using APIPetrack.Models;
using APIPetrack.Models.Custom;

namespace APIPetrack.Services
{
    public interface IAuthorizationServices
    {
        Task<AuthorizationResponse> LoginPetOwnerAsync(PetOwner.LoginPetOwner loginPetOwner);
        Task<AuthorizationResponse> LoginPetShoreShelterAsync(PetStoreShelter.LoginPetStoreShelter loginPetShoreShelter);
        Task<AuthorizationResponse> LoginVeterinarianAsync(Veterinarian.LoginVeterinarian loginVeterinarian);
    }
}
