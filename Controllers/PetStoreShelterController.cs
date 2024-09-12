using APIPetrack.Context;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using APIPetrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static APIPetrack.Models.Users.PetStoreShelter;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetStoreShelterController : Controller
    {
        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;

        public PetStoreShelterController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(PetStoreShelter petStoreShelter)
        {

            if (petStoreShelter == null || !ModelState.IsValid)
            {
                return BadRequest(petStoreShelter == null ? "Invalid pet store shelter data." : ModelState);
            }

            try
            {
                if (await _context.PetOwner.AnyAsync(po => po.Email == petStoreShelter.Email))
                {
                    return Conflict(new { message = "Email already in use." });
                }

                petStoreShelter.Password = _passwordHasher.HashPassword(petStoreShelter.Password);

                _context.PetStoreShelter.Add(petStoreShelter);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account created successfully." });
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is Exception)
            {
                var errorMessage = ex is DbUpdateException ? "An error occurred while creating the account." : "An unexpected error occurred.";
                return StatusCode(500, new { message = errorMessage, details = ex.Message });
            }

        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] PetStoreShelter.LoginPetStoreShelter loginPetStoreShelter)
        {
            var response = await _authorizationService.LoginPetShoreShelterAsync(loginPetStoreShelter);

            if (response.Result)
            {
                var petStoreShelter = await _context.PetStoreShelter.FirstOrDefaultAsync(po => po.Email == loginPetStoreShelter.Email);

                if (petStoreShelter != null)
                {
                    var result = new
                    {
                        response.Result,
                        response.Token,
                        PetStoreShelterId = petStoreShelter.Id,
                        PetStoreShelterName = petStoreShelter.Name,
                        PetStoreShelterEmail = petStoreShelter.Email,
                        PetStoreShelterAddress = petStoreShelter.Address,
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

        //[Authorize]
        [HttpGet("List")]
        public async Task<IActionResult> ListPetStoreShelter()
        {
            var list = await _context.PetStoreShelter.Select(p => new { p.Id, p.Name, p.Address, p.Email }).ToListAsync();

            if (list.Count == 0)
            {
                return NoContent();
            }

            return Ok(list);
        }

        //[Authorize]
        [HttpPut("UpdateAccount/{id}")]
        public async Task<IActionResult> UpdateAccount(int id, UpdatePetStoreShelter petStoreShelter)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {

                var existingPetStoreShelter = await _context.PetStoreShelter.FindAsync(id);

                if (existingPetStoreShelter == null)
                {
                    return NotFound(new { message = "Pet store shelter not found." });
                }

                if (!string.IsNullOrEmpty(petStoreShelter.Email) && existingPetStoreShelter.Email != petStoreShelter.Email)
                {
                    if (await _context.PetStoreShelter.AnyAsync(ps => ps.Email == petStoreShelter.Email))
                    {
                        return Conflict(new { message = "Email already in use." });
                    }
                    existingPetStoreShelter.Email = petStoreShelter.Email;
                }

                if (!string.IsNullOrEmpty(petStoreShelter.Name))
                {
                    existingPetStoreShelter.Name = petStoreShelter.Name;
                }

                if (!string.IsNullOrEmpty(petStoreShelter.Address))
                {
                    existingPetStoreShelter.Address = petStoreShelter.Address;
                }

                if (!string.IsNullOrEmpty(petStoreShelter.Email))
                {
                    existingPetStoreShelter.Email = petStoreShelter.Email;
                }

                _context.PetStoreShelter.Update(existingPetStoreShelter);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the account.", details = ex.Message });
            }

        }

        //[Authorize]
        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var petStoreShelter = await _context.PetStoreShelter.FindAsync(id);

                if (petStoreShelter == null)
                {
                    return NotFound(new { message = "Pet store shelter not found." });
                }

                var passwordVerificationResult = _passwordHasher.VerifyPassword(model.CurrentPassword, petStoreShelter.Password);

                if (!passwordVerificationResult)
                {
                    return Unauthorized(new { message = "Incorrect current password." });
                }

                petStoreShelter.Password = _passwordHasher.HashPassword(model.NewPassword);

                _context.PetStoreShelter.Update(petStoreShelter);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the password.", details = ex.Message });
            }
        }

        //[Authorize]
        [HttpDelete("DeleteAccount/{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var petStoreShelter = await _context.PetStoreShelter.FindAsync(id);
                if (petStoreShelter == null)
                {
                    return NotFound(new { message = "Pet owner not found." });
                }

                _context.PetStoreShelter.Remove(petStoreShelter);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the account.", details = ex.Message });
            }
        }

    }
}
