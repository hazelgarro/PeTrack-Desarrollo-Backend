using APIPetrack.Context;
using APIPetrack.Models;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using APIPetrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIPetrack.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {

        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;
        private readonly EmailService _emailService;
        private readonly EmailTemplateService _emailTemplateService;

        public UserController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService, EmailService emailService, EmailTemplateService emailTemplateService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(RegisterUserRequest request)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Invalid model state.",
                    Data = ModelState
                });
            }

            try
            {

                if (_context.AppUser.Any(po => po.Email == request.Email))
                {
                    return Conflict(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Email already in use.",
                        Data = null
                    });
                }

                var user = new AppUser
                {
                    Email = request.Email,
                    Password = _passwordHasher.HashPassword(request.Password),
                    UserTypeId = request.UserTypeId,
                    ProfilePicture = request.ProfilePicture,
                    PhoneNumber = request.PhoneNumber,
                    ImagePublicId = request.ImagePublicId,
                };

                _context.AppUser.Add(user);
                _context.SaveChanges();

                var userCreated = SearchUser(user.Email);

                switch (request.UserTypeId)
                {
                    case 'O': // PetOwner

                        var namePetOwner = request.AdditionalData.TryGetValue("completeName", out var completeNamePetOwner);

                        var petOwner = new PetOwner
                        {
                            AppUserId = userCreated.Id,
                            CompleteName = completeNamePetOwner?.ToString()
                        };

                        _context.PetOwner.Add(petOwner);
                        break;

                    case 'S': // PetStoreShelter

                        var nameStore = request.AdditionalData.TryGetValue("name", out var namePetStore);
                        var adddressStore = request.AdditionalData.TryGetValue("address", out var addressStoreShelter);
                        var coverPictureStore = request.AdditionalData.TryGetValue("coverPicture", out var coverPicturePetStore);
                        var workingDaysStore = request.AdditionalData.TryGetValue("workingDays", out var workingDaysPetStore);
                        var workingHoursStore = request.AdditionalData.TryGetValue("workingHours", out var workingHoursPetStore);
                        var imagePublicCoverStore = request.AdditionalData.TryGetValue("imagePublicIdCover", out var imagePublicIdCoverStoreShelter);

                        var petStoreShelter = new PetStoreShelter
                        {
                            AppUserId = userCreated.Id,
                            Name = namePetStore?.ToString(),
                            Address = addressStoreShelter?.ToString(),
                            CoverPicture = coverPicturePetStore?.ToString(),
                            WorkingDays = workingDaysPetStore?.ToString(),
                            WorkingHours = workingHoursPetStore?.ToString(),
                            ImagePublicIdCover = imagePublicIdCoverStoreShelter?.ToString(),
                        };

                        _context.PetStoreShelter.Add(petStoreShelter);
                        break;

                    default:
                        return BadRequest(new ApiResponse<object>
                        {
                            Result = false,
                            Message = "Invalid UserTypeId.",
                            Data = null
                        });
                }

                _context.SaveChanges();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Account created successfully.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while creating the account.",
                    Data = ex.Message
                });
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
                                { "completeName", petOwner.CompleteName }
                            };
                            break;
                        case 'S': // PetStoreShelter
                            var petStoreShelter = SearchPetStoreShelter(user.Id);
                            details = new Dictionary<string, object>
                            {
                                { "name", petStoreShelter.Name },
                                { "address", petStoreShelter.Address },
                                { "coverPicture", petStoreShelter.CoverPicture },
                                { "imagePublicIdCover", petStoreShelter.ImagePublicIdCover },
                                { "workingDays", petStoreShelter.WorkingDays },
                                { "workingHours", petStoreShelter.WorkingHours }
                            };

                            break;

                        default:
                            return BadRequest(new ApiResponse<object>
                            {
                                Result = false,
                                Message = "Invalid UserTypeId.",
                                Data = null
                            });
                    }

                    var result = new
                    {
                        response.Token,
                        user.Id,
                        user.UserTypeId,
                        user.ProfilePicture,
                        user.ImagePublicId,
                        user.PhoneNumber,
                        details
                    };

                    return Ok(new ApiResponse<object>
                    {
                        Result = true,
                        Message = "Login successful.",
                        Data = result
                    });
                }

                return Unauthorized(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Invalid login credentials.",
                    Data = null
                });
            }
            else
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Login failed.",
                    Data = response
                });
            }
        }

        [HttpPost("VerifyLogin")]
        public IActionResult VerifyLogin([FromBody] TokenRequest tokenRequest)
        {
            var principal = _authorizationService.ValidateToken(tokenRequest.Token);

            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Result = false,
                    Message = "The user is not logged in",
                    Data = null
                });
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Result = false,
                    Message = "User ID not found in token",
                    Data = null
                });
            }

            int userId = int.Parse(userIdClaim.Value);

            var user = _context.AppUser.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Result = false,
                    Message = "User not found",
                    Data = null
                });
            }

            Dictionary<string, object> result = new Dictionary<string, object>{

                { "id", user.Id },
                { "email", user.Email },
                { "profilePicture", user.ProfilePicture },
                { "imagePublicId", user.ImagePublicId },
                { "userTypeId", user.UserTypeId },
                { "phoneNumber", user.PhoneNumber }
            };

            switch (user.UserTypeId)
            {
                case 'O': // PetOwner
                    var petOwner = SearchPetOwner(user.Id);
                    result.Add("userType", "PetOwner");
                    result.Add("completeName", petOwner.CompleteName);

                    break;
                case 'S': // PetStoreShelter
                    var petStoreShelter = SearchPetStoreShelter(user.Id);
                    if (petStoreShelter != null)
                    {
                        result.Add("userType", "PetStoreShelter");
                        result.Add("name", petStoreShelter.Name);
                        result.Add("address", petStoreShelter.Address);
                        result.Add("coverPicture", petStoreShelter.CoverPicture);
                        result.Add("imagePublicIdCover", petStoreShelter.ImagePublicIdCover);
                        result.Add("workingDays", petStoreShelter.WorkingDays);
                        result.Add("workingHours", petStoreShelter.WorkingHours);
                    }
                    break;
                default:
                    return BadRequest(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Invalid UserTypeId.",
                        Data = null
                    });
            }

            return Ok(new ApiResponse<object>
            {
                Result = true,
                Message = "The user is logged in",
                Data = result
            });
        }

        [Authorize]
        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Invalid input data.",
                    Data = ModelState
                });
            }

            try
            {
                var user = await _context.AppUser.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "User not found.",
                        Data = null
                    });
                }

                var passwordVerificationResult = _passwordHasher.VerifyPassword(model.CurrentPassword, user.Password);

                if (!passwordVerificationResult)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Incorrect current password.",
                        Data = null
                    });
                }

                user.Password = _passwordHasher.HashPassword(model.NewPassword);

                _context.AppUser.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Password updated successfully.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while updating the password.",
                    Data = ex.Message
                });
            }
        }

        [HttpPost("RequestPasswordReset")]
        public async Task<IActionResult> RequestPasswordReset(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Email is required.",
                    Data = null
                });
            }

            try
            {
                var user = await _context.AppUser.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Please check the provided email and try again.",
                        Data = null
                    });
                }

                var token = Guid.NewGuid().ToString();

                user.PasswordResetToken = token;
                user.TokenExpiration = DateTime.UtcNow.AddHours(1); // Expiración del token en 1 hora
                await _context.SaveChangesAsync();

                string encryptedToken = EmailService.Encrypt(token);

                string resetUrl = $"https://petrack-ten.vercel.app/ResetPassword?token={Uri.EscapeDataString(encryptedToken)}";

                string emailBody = _emailTemplateService.GetPasswordResetEmailBody(resetUrl);

                await _emailService.SendEmailAsync(email, "Reset Password", emailBody);

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "A password reset link has been sent to your email.",
                    Data = resetUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while requesting password reset.",
                    Data = ex.Message
                });
            }
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Invalid input data.",
                    Data = ModelState
                });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.Token))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Token cannot be null or empty.",
                        Data = null
                    });
                }

                string decodedToken = Uri.UnescapeDataString(model.Token);
                Console.WriteLine($"Decoded Token: {decodedToken}");

                string decryptedToken = EmailService.Decrypt(decodedToken);

                if (string.IsNullOrWhiteSpace(decryptedToken))
                {
                    Console.WriteLine("Failed to decrypt token after decoding.");
                    return BadRequest(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Failed to decrypt token.",
                        Data = null
                    });
                }

                var user = await _context.AppUser
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == decryptedToken && u.TokenExpiration > DateTime.UtcNow);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Invalid token or token has expired.",
                        Data = null
                    });
                }

                user.Password = _passwordHasher.HashPassword(model.NewPassword);
                user.PasswordResetToken = null;
                user.TokenExpiration = null;

                _context.AppUser.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Password reset successfully.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ResetPassword: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while resetting the password.",
                    Data = ex.Message
                });
            }
        }


        [HttpGet("DetailsUser/{id}")]
        public async Task<IActionResult> DetailsUser(int id)
        {
            var user = await _context.AppUser.FirstOrDefaultAsync(po => po.Id == id);

            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "User not found.",
                    Data = null
                });
            }

            Dictionary<string, object> details = new Dictionary<string, object>{

                { "id", user.Id },
                { "email", user.Email },
                { "profilePicture", user.ProfilePicture },
                { "imagePublicId", user.ImagePublicId },
                { "userTypeId", user.UserTypeId },
                { "phoneNumber", user.PhoneNumber }
            };

            switch (user.UserTypeId)
            {
                case 'O': // PetOwner
                    var petOwner = SearchPetOwner(user.Id);
                    if (petOwner != null)
                    {
                        details.Add("userType", "PetOwner");
                        details.Add("completeName", petOwner.CompleteName);
                    }
                    break;

                case 'S': // PetStoreShelter
                    var petStoreShelter = SearchPetStoreShelter(user.Id);
                    if (petStoreShelter != null)
                    {
                        details.Add("userType", "PetStoreShelter");
                        details.Add("name", petStoreShelter.Name);
                        details.Add("address", petStoreShelter.Address);
                        details.Add("coverPicture", petStoreShelter.CoverPicture);
                        details.Add("imagePublicIdCover", petStoreShelter.ImagePublicIdCover);
                        details.Add("workingDays", petStoreShelter.WorkingDays);
                        details.Add("workingHours", petStoreShelter.WorkingHours);
                    }
                    break;

                default:
                    return BadRequest(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Invalid UserTypeId.",
                        Data = null
                    });
            }

            return Ok(new ApiResponse<Dictionary<string, object>>
            {
                Result = true,
                Message = "User details retrieved successfully.",
                Data = details
            });
        }

        [HttpGet("ListPetStoreShelters")]
        public async Task<IActionResult> ListPetStoreShelters()
        {
            var petStoreShelterUsers = await _context.AppUser.Where(user => user.UserTypeId == 'S').ToListAsync();

            if (petStoreShelterUsers == null || petStoreShelterUsers.Count == 0)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "No pet store shelters found.",
                    Data = null
                });
            }

            var petStoreShelterDetailsList = new List<Dictionary<string, object>>();

            foreach (var user in petStoreShelterUsers)
            {
                var petStoreShelter = SearchPetStoreShelter(user.Id);

                if (petStoreShelter != null)
                {
                    var details = new Dictionary<string, object>{
                        { "id", user.Id },
                        { "email", user.Email },
                        { "name", petStoreShelter.Name },
                        { "profilePicture", user.ProfilePicture },
                        { "address", petStoreShelter.Address },
                        { "phoneNumber", user.PhoneNumber },
                        { "coverPicture", petStoreShelter.CoverPicture },
                        { "imagePublicIdCover", petStoreShelter.ImagePublicIdCover },
                        { "workingDays", petStoreShelter.WorkingDays },
                        { "workingHours", petStoreShelter.WorkingHours }
                    };

                    petStoreShelterDetailsList.Add(details);
                }
            }

            return Ok(new ApiResponse<List<Dictionary<string, object>>>
            {
                Result = true,
                Message = "Pet store shelters retrieved successfully.",
                Data = petStoreShelterDetailsList
            });

        }

        [Authorize]
        [HttpPut("EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] UpdateUser updatedUser)
        {
            var user = await _context.AppUser.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "User not found.",
                    Data = null
                });
            }

            var existingEmail = await _context.AppUser.FirstOrDefaultAsync(u => u.Email == updatedUser.Email && u.Id != id);
            if (existingEmail != null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Email already in use by another user.",
                    Data = null
                });
            }

            user.Email = updatedUser.Email;
            user.ProfilePicture = updatedUser.ProfilePicture;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.ImagePublicId = updatedUser.ImagePublicId;

            switch (user.UserTypeId)
            {
                case 'O': // PetOwner
                    var petOwner = await _context.PetOwner.FirstOrDefaultAsync(po => po.AppUserId == id);
                    if (petOwner != null)
                    {
                        petOwner.CompleteName = updatedUser.CompleteName;
                    }
                    break;

                case 'S': // PetStoreShelter
                    var petStoreShelter = await _context.PetStoreShelter.FirstOrDefaultAsync(s => s.AppUserId == id);
                    if (petStoreShelter != null)
                    {
                        petStoreShelter.Name = updatedUser.Name;
                        petStoreShelter.Address = updatedUser.Address;
                        petStoreShelter.CoverPicture = updatedUser.CoverPicture;
                        petStoreShelter.ImagePublicIdCover = updatedUser.ImagePublicIdCover;
                        petStoreShelter.WorkingDays = updatedUser.WorkingDays;
                        petStoreShelter.WorkingHours = updatedUser.WorkingHours;
                    }
                    break;

                default:
                    return BadRequest(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Invalid UserTypeId.",
                        Data = null
                    });
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Result = true,
                Message = "User and type-specific details updated successfully.",
                Data = null
            });
        }

        [Authorize]
        [HttpDelete("DeleteAccount/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.AppUser
                    .Include(u => u.PetOwner)
                    .Include(u => u.PetStoreShelter)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "User not found.",
                        Data = null
                    });
                }

                var notifications = await _context.Notification
                    .Where(n => n.UserId == id)
                    .ToListAsync();

                if (notifications.Any())
                {
                    _context.Notification.RemoveRange(notifications);
                }

                _context.AppUser.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "User deleted successfully.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while deleting the user.",
                    Data = new { details = ex.Message }
                });
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

        [HttpGet("SearchPetStoreShelter")]
        public PetStoreShelter SearchPetStoreShelter(int id)
        {
            var petStoreShelter = new PetStoreShelter();
            petStoreShelter = _context.PetStoreShelter.FirstOrDefault(po => po.AppUserId == id);
            return petStoreShelter;
        }

    }
}
