using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    public class EnrolmentsController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Enters the logged-in Participant into one category of an event.</summary>
        /// <remarks>
        /// Stores the link between the participant, the event (through the category) and the chosen category.
        /// A participant can hold only one active enrolment per event.
        /// </remarks>
        /// <response code="201">The new enrolment.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="404">Event or category not found.</response>
        /// <response code="409">Duplicate entry, registration closed, event not open, category inactive, or category full.</response>
        [HttpPost("api/events/{eventId:int}/enrolments")]
        [Authorize(Roles = Roles.Participant)]
        [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int eventId, EnrolmentRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null || ev.Status == "Draft") return NotFound(new { message = "Event not found." });

            var category = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);
            if (category == null) return NotFound(new { message = "Category not found for this event." });

            if (ev.Status != "Published")
                return Conflict(new { message = "This event is not open for enrolment." });
            if (!category.IsActive)
                return Conflict(new { message = "This category is not available." });
            if (DateOnly.FromDateTime(DateTime.UtcNow) > ev.RegistrationCloseDate)
                return Conflict(new { message = "Registration for this event has closed." });

            var userId = CurrentUserId;

            var alreadyEntered = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantId == userId && e.Status != "Cancelled" &&
                e.Category != null && e.Category.EventId == eventId);
            if (alreadyEntered)
                return Conflict(new { message = "You are already enrolled in this event." });

            var activeCount = await _context.Enrolments
                .CountAsync(e => e.CategoryId == category.CategoryId && e.Status != "Cancelled");
            if (activeCount >= category.Capacity)
                return Conflict(new { message = "This category is full." });

            // A cancelled enrolment for the same category is re-activated (the unique index allows one row per participant and category).
            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.ParticipantId == userId && e.CategoryId == category.CategoryId);
            if (enrolment == null)
            {
                enrolment = new Enrolment { ParticipantId = userId, CategoryId = category.CategoryId };
                _context.Enrolments.Add(enrolment);
            }
            enrolment.EnrolmentDate = DateTime.UtcNow;
            enrolment.EmergencyContactName = dto.EmergencyContactName.Trim();
            enrolment.EmergencyContactPhone = dto.EmergencyContactPhone.Trim();
            enrolment.Status = "Confirmed";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "You are already enrolled in this category." });
            }

            var created = await _context.Enrolments.AsNoTracking().WithDetails()
                .FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
            return CreatedAtAction(nameof(GetEnrolment), new { enrolmentId = created.EnrolmentId }, created.ToDto());
        }

        /// <summary>Lists all enrolments for an event owned by the logged-in Organiser.</summary>
        /// <response code="200">The enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("api/events/{eventId:int}/enrolments")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var enrolments = await _context.Enrolments.AsNoTracking().WithDetails()
                .Where(e => e.Category != null && e.Category.EventId == eventId)
                .OrderBy(e => e.EnrolmentDate)
                .ToListAsync();
            return Ok(enrolments.Select(e => e.ToDto()));
        }

        /// <summary>Returns one enrolment. Allowed for the participant who owns it and the organiser of its event.</summary>
        /// <response code="200">The enrolment.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not the owning participant or event organiser.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpGet("api/enrolments/{enrolmentId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolment(int enrolmentId)
        {
            var enrolment = await _context.Enrolments.AsNoTracking().WithDetails()
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            var isParticipantOwner = enrolment.ParticipantId == CurrentUserId;
            var isEventOrganiser = enrolment.Category?.Event != null && IsOwnerOrganiser(enrolment.Category.Event.OrganiserId);
            if (!isParticipantOwner && !isEventOrganiser)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not permitted to view this enrolment." });

            return Ok(enrolment.ToDto());
        }

        /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
        /// <response code="200">The cancelled enrolment.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser, or the enrolment belongs to someone else.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">Already cancelled, or a result has already been recorded.</response>
        [HttpPut("api/enrolments/{enrolmentId:int}/cancel")]
        [Authorize(Roles = Roles.Participant)]
        [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelEnrolment(int enrolmentId)
        {
            var enrolment = await _context.Enrolments.WithDetails()
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
            if (enrolment.ParticipantId != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This is not your enrolment." });

            if (enrolment.Status == "Cancelled")
                return Conflict(new { message = "This enrolment is already cancelled." });
            if (await _context.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
                return Conflict(new { message = "A result has been recorded, so this enrolment can no longer be cancelled." });

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return Ok(enrolment.ToDto());
        }
    }
}
