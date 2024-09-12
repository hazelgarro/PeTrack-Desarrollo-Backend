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
    public class VeterinarianController : Controller
    {

        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;
        public VeterinarianController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(Veterinarian veterinarian)
        {

            if (veterinarian == null || !ModelState.IsValid)
            {
                return BadRequest(veterinarian == null ? "Invalid veterinarian data." : ModelState);
            }

            try
            {
                if (await _context.PetOwner.AnyAsync(po => po.Email == veterinarian.Email))
                {
                    return Conflict(new { message = "Email already in use." });
                }

                veterinarian.Password = _passwordHasher.HashPassword(veterinarian.Password);

                _context.Veterinarian.Add(veterinarian);
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
        public async Task<IActionResult> Login([FromBody] Veterinarian.LoginVeterinarian loginVeterinarian)
        {
            var response = await _authorizationService.LoginVeterinarianAsync(loginVeterinarian);

            if (response.Result)
            {
                var veterinarian = await _context.Veterinarian.FirstOrDefaultAsync(v => v.Email == loginVeterinarian.Email);

                if (veterinarian != null)
                {
                    var result = new
                    {
                        response.Result,
                        response.Token,
                        VeterinarianId = veterinarian.Id,
                        VeterinarianFirstName = veterinarian.FirstName,
                        VeterinarianLastName = veterinarian.LastName,
                        VeterinarianEmail = veterinarian.Email,
                        VeterinarianClinic = veterinarian.ClinicName,
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
        public async Task<IActionResult> ListVeterinarians()
        {
            var list = await _context.Veterinarian.Select(v => new { v.Id, v.FirstName, v.LastName, v.Email, v.ClinicName }).ToListAsync();

            if (list.Count == 0)
            {
                return NoContent();
            }

            return Ok(list);
        }

        //[Authorize]
        [HttpPut("UpdateAccount/{id}")]
        public async Task<IActionResult> UpdateAccount(int id, UpdateVeterinarian veterinarian)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {

                var existingVeterinarian = await _context.Veterinarian.FindAsync(id);

                if (existingVeterinarian == null)
                {
                    return NotFound(new { message = "Veterinarian not found." });
                }


                if (!string.IsNullOrEmpty(veterinarian.Email) && existingVeterinarian.Email != veterinarian.Email)
                {
                    if (await _context.Veterinarian.AnyAsync(v => v.Email == veterinarian.Email))
                    {
                        return Conflict(new { message = "Email already in use." });
                    }
                    existingVeterinarian.Email = veterinarian.Email;
                }

                if (!string.IsNullOrEmpty(veterinarian.FirstName))
                {
                    existingVeterinarian.FirstName = veterinarian.FirstName;
                }

                if (!string.IsNullOrEmpty(veterinarian.LastName))
                {
                    existingVeterinarian.LastName = veterinarian.LastName;
                }

                _context.Veterinarian.Update(existingVeterinarian);
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
                var veterinarian = await _context.Veterinarian.FindAsync(id);

                if (veterinarian == null)
                {
                    return NotFound(new { message = "Veterinarian not found." });
                }

                var passwordVerificationResult = _passwordHasher.VerifyPassword(model.CurrentPassword, veterinarian.Password);

                if (!passwordVerificationResult)
                {
                    return Unauthorized(new { message = "Incorrect current password." });
                }

                veterinarian.Password = _passwordHasher.HashPassword(model.NewPassword);

                _context.Veterinarian.Update(veterinarian);
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
                var veterinarian = await _context.Veterinarian.FindAsync(id);
                if (veterinarian == null)
                {
                    return NotFound(new { message = "Veterinarian not found." });
                }

                _context.Veterinarian.Remove(veterinarian);
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
