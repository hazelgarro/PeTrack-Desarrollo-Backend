using APIPetrack.Context;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace APIPetrack.Services
{
    public class AuthorizationServices : IAuthorizationServices
    {
        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthorizationServices(DbContextPetrack context, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<AuthorizationResponse> LoginUserAsync(AppUser.LoginUser loginUser)
        {
            var user = await _context.AppUser.FirstOrDefaultAsync(u => u.Email == loginUser.Email);

            if (user == null)
            {
                return new AuthorizationResponse
                {
                    Token = null,
                    Result = false,
                    Message = "User not found"
                };
            }

            bool passwordIsValid = _passwordHasher.VerifyPassword(loginUser.Password, user.Password);

            if (!passwordIsValid)
            {
                return new AuthorizationResponse
                {
                    Token = null,
                    Result = false,
                    Message = "Incorrect password"
                };
            }

            string tokenCreated = GenerateToken(user.Id);

            return new AuthorizationResponse
            {
                Token = tokenCreated,
                Result = true,
                Message = "Ok"
            };
        }
        private string GenerateToken(int userId)
        {
            var key = _configuration.GetValue<string>("JwtSettings:Key");
            var keyBytes = Encoding.ASCII.GetBytes(key);

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
