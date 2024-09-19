using APIPetrack.Context;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using APIPetrack.Services;
using Microsoft.AspNetCore.Authorization;
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

                        var clinic = request.AdditionalData.TryGetValue("Name", out var NameVet);
                        var adddress = request.AdditionalData.TryGetValue("Address", out var addressVet);
                        var coverPictureVet = request.AdditionalData.TryGetValue("CoverPicture", out var coverPictureVeterinarian);
                        var workingDaysVet = request.AdditionalData.TryGetValue("WorkingDays", out var workingDaysVeterinarian);
                        var workingHoursVet = request.AdditionalData.TryGetValue("WorkingHours", out var workingHoursVeterinarian);

                        var veterinarian = new Veterinarian
                        {
                            AppUserId = userCreated.Id,
                            Name = NameVet?.ToString(),
                            Address = addressVet?.ToString(),
                            CoverPicture = coverPictureVeterinarian?.ToString(),
                            WorkingDays = workingDaysVeterinarian?.ToString(),
                            WorkingHours = workingHoursVeterinarian?.ToString()
                        };

                        _context.Veterinarian.Add(veterinarian);
                        break;

                    case 'S': // PetStoreShelter

                        var nameStore = request.AdditionalData.TryGetValue("Name", out var namePetStore);
                        var adddressStore = request.AdditionalData.TryGetValue("Address", out var addressStoreShelter);
                        var coverPictureStore = request.AdditionalData.TryGetValue("CoverPicture", out var coverPicturePetStore);
                        var workingDaysStore = request.AdditionalData.TryGetValue("WorkingDays", out var workingDaysPetStore);
                        var workingHoursStore = request.AdditionalData.TryGetValue("WorkingHours", out var workingHoursPetStore);

                        var petStoreShelter = new PetStoreShelter
                        {
                            AppUserId = userCreated.Id,
                            Name = namePetStore?.ToString(),
                            Address = addressStoreShelter?.ToString(),
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
                                { "Name", veterinarian.Name },
                                { "Address", veterinarian.Address },
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

        [Authorize]
        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _context.AppUser.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var passwordVerificationResult = _passwordHasher.VerifyPassword(model.CurrentPassword, user.Password);

                if (!passwordVerificationResult)
                {
                    return Unauthorized(new { message = "Incorrect current password." });
                }

                user.Password = _passwordHasher.HashPassword(model.NewPassword);

                _context.AppUser.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the password.", details = ex.Message });
            }
        }

        [HttpGet("DetailsUser/{id}")]
        public async Task<IActionResult> DetailsUser(int id)
        {
            var user = await _context.AppUser.FirstOrDefaultAsync(po => po.Id == id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            Dictionary<string, object> details = new Dictionary<string, object>{

                { "Id", user.Id },
                { "Email", user.Email },
                { "ProfilePicture", user.ProfilePicture },
                { "UserTypeId", user.UserTypeId },
                { "PhoneNumber", user.PhoneNumber }
            };

            switch (user.UserTypeId)
            {
                case 'O': // PetOwner
                    var petOwner = SearchPetOwner(user.Id);
                    if (petOwner != null)
                    {
                        details.Add("UserType", "PetOwner");
                        details.Add("CompleteName", petOwner.CompleteName);
                    }
                    break;

                case 'V': // Veterinarian
                    var veterinarian = SearchVeterinarian(user.Id);
                    if (veterinarian != null)
                    {
                        details.Add("UserType", "Veterinarian");
                        details.Add("Name", veterinarian.Name);
                        details.Add("CoverPicture", veterinarian.CoverPicture);
                        details.Add("Address", veterinarian.Address);
                        details.Add("WorkingDays", veterinarian.WorkingDays);
                        details.Add("WorkingHours", veterinarian.WorkingHours);
                    }
                    break;

                case 'S': // PetStoreShelter
                    var petStoreShelter = SearchPetStoreShelter(user.Id);
                    if (petStoreShelter != null)
                    {
                        details.Add("UserType", "PetStoreShelter");
                        details.Add("Name", petStoreShelter.Name);
                        details.Add("Address", petStoreShelter.Address);
                        details.Add("CoverPicture", petStoreShelter.CoverPicture);
                        details.Add("WorkingDays", petStoreShelter.WorkingDays);
                        details.Add("WorkingHours", petStoreShelter.WorkingHours);
                    }
                    break;

                default:
                    return BadRequest("Invalid UserTypeId.");
            }

            return Ok(details);
        }

        [HttpGet("ListVeterinarians")]
        public async Task<IActionResult> ListVeterinarians()
        {
            var veterinarianUsers = await _context.AppUser.Where(user => user.UserTypeId == 'V').ToListAsync();

            if (veterinarianUsers == null || veterinarianUsers.Count == 0)
            {
                return NotFound("No veterinarians found.");
            }

            var veterinarianDetailsList = new List<Dictionary<string, object>>();

            foreach (var user in veterinarianUsers)
            {
                var veterinarian = SearchVeterinarian(user.Id);

                if (veterinarian != null)
                {
                    var details = new Dictionary<string, object>{
                        { "Id", user.Id },
                        { "Email", user.Email },
                        { "Name", veterinarian.Name },
                        { "ProfilePicture", user.ProfilePicture },
                        { "CoverPicture", veterinarian.CoverPicture },
                        { "PhoneNumber", user.PhoneNumber },
                        { "WorkingDays", veterinarian.WorkingDays },
                        { "WorkingHours", veterinarian.WorkingHours }
                    };

                    veterinarianDetailsList.Add(details);
                }
            }

            return Ok(veterinarianDetailsList);

        }

        [HttpGet("ListPetStoreShelters")]
        public async Task<IActionResult> ListPetStoreShelters()
        {
            var petStoreShelterUsers = await _context.AppUser.Where(user => user.UserTypeId == 'S').ToListAsync();

            if (petStoreShelterUsers == null || petStoreShelterUsers.Count == 0)
            {
                return NotFound("No pet store shelters found.");
            }

            var petStoreShelterDetailsList = new List<Dictionary<string, object>>();

            foreach (var user in petStoreShelterUsers)
            {
                var petStoreShelter = SearchPetStoreShelter(user.Id);

                if (petStoreShelter != null)
                {
                    var details = new Dictionary<string, object>{
                        { "Id", user.Id },
                        { "Email", user.Email },
                        { "Name", petStoreShelter.Name },
                        { "ProfilePicture", user.ProfilePicture },
                        { "Address", petStoreShelter.Address },
                        { "PhoneNumber", user.PhoneNumber },
                        { "CoverPicture", petStoreShelter.CoverPicture },
                        { "WorkingDays", petStoreShelter.WorkingDays },
                        { "WorkingHours", petStoreShelter.WorkingHours }
                    };

                    petStoreShelterDetailsList.Add(details);
                }
            }

            return Ok(petStoreShelterDetailsList);

        }

        [Authorize]
        [HttpPut("EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] UpdateUser updatedUser)
        {
            var user = await _context.AppUser.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var existingEmail = await _context.AppUser.FirstOrDefaultAsync(u => u.Email == updatedUser.Email && u.Id != id);
            if (existingEmail != null)
            {
                return BadRequest("Email already in use by another user.");
            }

            user.Email = updatedUser.Email;
            user.ProfilePicture = updatedUser.ProfilePicture;
            user.PhoneNumber = updatedUser.PhoneNumber;

            switch (user.UserTypeId)
            {
                case 'O': // PetOwner
                    var petOwner = await _context.PetOwner.FirstOrDefaultAsync(po => po.AppUserId == id);
                    if (petOwner != null)
                    {
                        petOwner.CompleteName = updatedUser.CompleteName;
                    }
                    break;

                case 'V': // Veterinarian
                    var veterinarian = await _context.Veterinarian.FirstOrDefaultAsync(v => v.AppUserId == id);
                    if (veterinarian != null)
                    {
                        veterinarian.Name = updatedUser.Name;
                        veterinarian.Address = updatedUser.Address;
                        veterinarian.CoverPicture = updatedUser.CoverPicture;
                        veterinarian.WorkingDays = updatedUser.WorkingDays;
                        veterinarian.WorkingHours = updatedUser.WorkingHours;
                    }
                    break;

                case 'S': // PetStoreShelter
                    var petStoreShelter = await _context.PetStoreShelter.FirstOrDefaultAsync(s => s.AppUserId == id);
                    if (petStoreShelter != null)
                    {
                        petStoreShelter.Name = updatedUser.Name;
                        petStoreShelter.Address = updatedUser.Address;
                        petStoreShelter.CoverPicture = updatedUser.CoverPicture;
                        petStoreShelter.WorkingDays = updatedUser.WorkingDays;
                        petStoreShelter.WorkingHours = updatedUser.WorkingHours;
                    }
                    break;

                default:
                    return BadRequest("Invalid UserTypeId.");
            }

            await _context.SaveChangesAsync();

            return Ok("User and type specific details updated successfully.");
        }

        [Authorize]
        [HttpDelete("DeleteAccount/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.AppUser
                    .Include(u => u.PetOwner)
                    .Include(u => u.Veterinarian)
                    .Include(u => u.PetStoreShelter)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound();
                }

                _context.AppUser.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the user.", details = ex.Message });
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
