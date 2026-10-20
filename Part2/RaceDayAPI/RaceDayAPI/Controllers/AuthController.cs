using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        public AuthController(RaceDayDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>Creates a Participant or Organiser account.</summary>
        /// <remarks>Organisers must also send the secret organiser code, so nobody can make themselves an Organiser.</remarks>
        /// <response code="201">Account created. Returns the user profile.</response>
        /// <response code="400">Invalid data or role.</response>
        /// <response code="403">Organiser code missing or wrong.</response>
        /// <response code="409">Email already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Role != Roles.Organiser && dto.Role != Roles.Participant)
                return BadRequest(new { message = "Role must be Organiser or Participant." });

            if (dto.Role == Roles.Organiser)
            {
                var expected = _config["OrganiserRegistrationCode"];
                if (string.IsNullOrEmpty(expected) || dto.OrganiserCode != expected)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "A valid organiser code is required to register as an Organiser." });
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { message = "Email is already registered." });

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role
            };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);   // never store the plain password

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, ToProfile(user));
        }

        /// <summary>Logs in and starts a cookie session holding the user's id, email and role as claims.</summary>
        /// <response code="200">Logged in. The auth cookie is set on the response.</response>
        /// <response code="400">Invalid request body.</response>
        /// <response code="401">Wrong email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
                return Unauthorized();

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized();

            // Claims: single labelled facts about the user, stored (encrypted) in the cookie.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Ok(ToProfile(user));
        }

        /// <summary>Logs out and clears the auth cookie.</summary>
        /// <response code="204">Logged out.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        private static UserProfileDto ToProfile(User u) => new UserProfileDto
        {
            UserId = u.UserId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role
        };
    }
}
