using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]   // any logged-in user, either role
    public class UsersController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Returns the profile of the logged-in user.</summary>
        /// <remarks>The user id comes from the cookie claims, never from the route.</remarks>
        /// <response code="200">The user's profile.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMe()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return Unauthorized();

            return Ok(ToProfile(user));
        }

        /// <summary>Updates the first name, last name and phone number of the logged-in user.</summary>
        /// <response code="200">The updated profile.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return Unauthorized();

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber;
            await _context.SaveChangesAsync();

            return Ok(ToProfile(user));
        }

        /// <summary>Lists the logged-in Participant's enrolments.</summary>
        /// <response code="200">The enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("me/enrolments")]
        [Authorize(Roles = Roles.Participant)]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = CurrentUserId;
            var enrolments = await _context.Enrolments.AsNoTracking().WithDetails()
                .Where(e => e.ParticipantId == userId)
                .OrderByDescending(e => e.EnrolmentDate)
                .ToListAsync();
            return Ok(enrolments.Select(e => e.ToDto()));
        }

        /// <summary>Lists the logged-in Participant's own results.</summary>
        /// <response code="200">The results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("me/results")]
        [Authorize(Roles = Roles.Participant)]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = CurrentUserId;
            var results = await _context.Results.AsNoTracking().WithDetails()
                .Where(r => r.Enrolment!.ParticipantId == userId)
                .OrderByDescending(r => r.RecordedAt)
                .ToListAsync();
            return Ok(results.Select(r => r.ToDto()));
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
