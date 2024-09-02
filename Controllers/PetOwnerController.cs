using APIPetrack.Context;
using APIPetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIPetrack.Services;
using static APIPetrack.Models.PetOwner;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetOwnerController : Controller
    {

        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;

        public PetOwnerController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(PetOwner petOwner) {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (await _context.PetOwner.AnyAsync(po => po.Email == petOwner.Email))
                {
                    return Conflict(new { message = "Email already in use." });
                }

                petOwner.Password = _passwordHasher.HashPassword(petOwner.Password);

                _context.PetOwner.Add(petOwner);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the account.", details = ex.Message });
            }

        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] PetOwner.LoginPetOwner loginPetOwner)
        {
            var response = await _authorizationService.LoginPetOwnerAsync(loginPetOwner);

            if (response.Result)
            {
                return Ok(response);
            }
            else
            {
                return Unauthorized(response);
            }
        }

    }
}
