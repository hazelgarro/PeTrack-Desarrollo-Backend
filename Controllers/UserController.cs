using APIPetrack.Context;
using APIPetrack.Models.Users;
using APIPetrack.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {

        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;

        public UserController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(RegisterUserRequest request)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {

                if (_context.AppUser.Any(po => po.Email == request.Email))
                {
                    return Conflict(new { message = "Email already in use." });
                }

                var user = new AppUser
                {
                    Email = request.Email,
                    Password = _passwordHasher.HashPassword(request.Password),
                    UserTypeId = request.UserTypeId,
                    ProfilePicture = request.ProfilePicture,
                    PhoneNumber = request.PhoneNumber
                };

                _context.AppUser.Add(user);
                _context.SaveChanges();

                var userCreated = SearchUser(user.Email);

                switch (request.UserTypeId)
                {
                    case 'O': // PetOwner

                        var namePetOwner = request.AdditionalData.TryGetValue("CompleteName", out var completeNamePetOwner);

                        var petOwner = new PetOwner
                        {
                            AppUserId = userCreated.Id,
                            CompleteName = completeNamePetOwner?.ToString()
                        };

                        _context.PetOwner.Add(petOwner);
                        break;

                    case 'V': // Veterinarian

                        var nameVeterinarian = request.AdditionalData.TryGetValue("CompleteName", out var completeNameVeterinarian);
                        var clinic = request.AdditionalData.TryGetValue("ClinicName", out var clinicName);
                        var coverPictureVet = request.AdditionalData.TryGetValue("CoverPicture", out var coverPictureVeterinarian);
                        var workingDaysVet = request.AdditionalData.TryGetValue("WorkingDays", out var workingDaysVeterinarian);
                        var workingHoursVet = request.AdditionalData.TryGetValue("WorkingHours", out var workingHoursVeterinarian);

                        var veterinarian = new Veterinarian
                        {
                            AppUserId = userCreated.Id,
                            CompleteName = completeNameVeterinarian?.ToString(),
                            ClinicName = clinicName?.ToString(),
                            CoverPicture = coverPictureVeterinarian?.ToString(),
                            WorkingDays = workingDaysVeterinarian?.ToString(),
                            WorkingHours = workingHoursVeterinarian?.ToString()
                        };

                        _context.Veterinarian.Add(veterinarian);
                        break;

                    case 'S': // PetStoreShelter

                        var nameStore = request.AdditionalData.TryGetValue("Name", out var namePetStore);
                        var adddress = request.AdditionalData.TryGetValue("Address", out var addressVet);
                        var coverPictureStore = request.AdditionalData.TryGetValue("CoverPicture", out var coverPicturePetStore);
                        var workingDaysStore = request.AdditionalData.TryGetValue("WorkingDays", out var workingDaysPetStore);
                        var workingHoursStore = request.AdditionalData.TryGetValue("WorkingHours", out var workingHoursPetStore);

                        var petStoreShelter = new PetStoreShelter
                        {
                            AppUserId = userCreated.Id,
                            Name = namePetStore?.ToString(),
                            Address = addressVet?.ToString(),
                            CoverPicture = coverPicturePetStore?.ToString(),
                            WorkingDays = workingDaysPetStore?.ToString(),
                            WorkingHours = workingHoursPetStore?.ToString()
                        };

                        _context.PetStoreShelter.Add(petStoreShelter);
                        break;

                    default:
                        return BadRequest("Invalid UserTypeId.");
                }

                _context.SaveChanges();
                return Ok(new { message = "Account created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the account.", details = ex.Message });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] AppUser.LoginUser loginUser)
        {
            var response = await _authorizationService.LoginUserAsync(loginUser);

            if (response.Result)
            {
                var user = await _context.AppUser.FirstOrDefaultAsync(po => po.Email == loginUser.Email);

                if (user != null)
                {
                    Dictionary<string, object> details = null;

                    switch (user.UserTypeId)
                    {
                        case 'O': // PetOwner
                            var petOwner = SearchPetOwner(user.Id);
                            details = new Dictionary<string, object>
                            {
                                { "CompleteName", petOwner.CompleteName }
                            };
                            break;
                        case 'V': // Veterinarian
                            var veterinarian = SearchVeterinarian(user.Id);
                            details = new Dictionary<string, object>
                            {
                                { "CompleteName", veterinarian.CompleteName },
                                { "ClinicName", veterinarian.ClinicName },
                                { "CoverPicture", veterinarian.CoverPicture },
                                { "WorkingDays", veterinarian.WorkingDays },
                                { "WorkingHours", veterinarian.WorkingHours }
                            };
                            break;
                        case 'S': // PetStoreShelter
                            var petStoreShelter = SearchPetStoreShelter(user.Id);
                            details = new Dictionary<string, object>
                            {
                                { "Name", petStoreShelter.Name },
                                { "Address", petStoreShelter.Address },
                                { "CoverPicture", petStoreShelter.CoverPicture },
                                { "WorkingDays", petStoreShelter.WorkingDays },
                                { "WorkingHours", petStoreShelter.WorkingHours }
                            };

                            break;

                        default:
                            return BadRequest("Invalid UserTypeId.");
                    }

                    var result = new
                    {
                        response.Result,
                        response.Token,
                        user.Id,
                        user.UserTypeId,
                        user.ProfilePicture,
                        user.PhoneNumber,
                        details
                    };

                    return Ok(result);
                }

                return Unauthorized(new { message = "Invalid login credentials." });
            }
            else
            {
                return Unauthorized(response);
            }
        }

        [HttpGet("SearchUser")]
        public AppUser SearchUser(string email)
        {
            var user = new AppUser();
            user = _context.AppUser.FirstOrDefault(po => po.Email == email);
            return user;
        }

        [HttpGet("SearchPetOwner")]
        public PetOwner SearchPetOwner(int id)
        {
            var petOwner = new PetOwner();
            petOwner = _context.PetOwner.FirstOrDefault(po => po.AppUserId == id);
            return petOwner;
        }

        [HttpGet("SearchVeterinarian")]
        public Veterinarian SearchVeterinarian(int id)
        {
            var veterinarian = new Veterinarian();
            veterinarian = _context.Veterinarian.FirstOrDefault(po => po.AppUserId == id);
            return veterinarian;
        }

        [HttpGet("SearchPetStoreShelter")]
        public PetStoreShelter SearchPetStoreShelter(int id)
        {
            var petStoreShelter = new PetStoreShelter();
            petStoreShelter = _context.PetStoreShelter.FirstOrDefault(po => po.AppUserId == id);
            return petStoreShelter;
        }

    }
}
