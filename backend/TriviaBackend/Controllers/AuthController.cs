using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Text;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TriviaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(TriviaDbContext _context, IConfiguration configuration) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<BaseUser>> Register(BaseUserDTO request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return Conflict("Username already exists");
            }

            BaseUser user = new();
            var hashedPassword = new PasswordHasher<BaseUser>().HashPassword(user, request.Password);
            user.Username = request.Username;
            user.PasswordHash = hashedPassword;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(BaseUserDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return BadRequest("Incorrect credentials");
            }
            if (new PasswordHasher<BaseUser>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return BadRequest("Incorrect credentials");
            }
            return CreateToken(user);
        }

        private string CreateToken(BaseUser user)
        {
            var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id),
        };
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
