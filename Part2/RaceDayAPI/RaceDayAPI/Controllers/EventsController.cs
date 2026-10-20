using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Lists public events (everything except Drafts), with optional filters.</summary>
        /// <param name="search">Matches event name, venue or city.</param>
        /// <param name="eventType">Running, Walking or Cycling.</param>
        /// <param name="fromDate">Only events on or after this date (yyyy-MM-dd).</param>
        /// <param name="toDate">Only events on or before this date (yyyy-MM-dd).</param>
        /// <response code="200">The matching events.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? search,
            [FromQuery] string? eventType,
            [FromQuery(Name = "from")] DateOnly? fromDate,
            [FromQuery(Name = "to")] DateOnly? toDate)
        {
            var query = _context.Events.AsNoTracking().Where(e => e.Status != "Draft");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(e => e.Name.Contains(s) || e.Venue.Contains(s) || e.City.Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.EventType == eventType);
            if (fromDate.HasValue)
                query = query.Where(e => e.EventDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(e => e.EventDate <= toDate.Value);

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();
            return Ok(events.Select(e => e.ToSummary()));
        }

        /// <summary>Lists the events created by the logged-in Organiser (including Drafts).</summary>
        /// <response code="200">The organiser's events.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpGet("mine")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(IEnumerable<EventSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = CurrentUserId;
            var events = await _context.Events.AsNoTracking()
                .Where(e => e.OrganiserId == userId)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
            return Ok(events.Select(e => e.ToSummary()));
        }

        /// <summary>Returns one event with its organiser, categories, route and latest forecast.</summary>
        /// <response code="200">The event detail.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{eventId:int}")]
        [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int eventId)
        {
            var ev = await _context.Events.AsNoTracking()
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                .Include(e => e.Route)
                .Include(e => e.WeatherForecasts)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            // Drafts are only visible to the organiser who owns them.
            if (ev == null || (ev.Status == "Draft" && !IsOwnerOrganiser(ev.OrganiserId)))
                return NotFound(new { message = "Event not found." });

            var isOwner = IsOwnerOrganiser(ev.OrganiserId);
            var latest = ev.WeatherForecasts
                .OrderByDescending(w => w.RetrievedAt).ThenByDescending(w => w.ForecastDate)
                .FirstOrDefault();

            var dto = new EventDetailDto
            {
                EventId = ev.EventId,
                OrganiserId = ev.OrganiserId,
                Name = ev.Name,
                EventType = ev.EventType,
                EventDate = ev.EventDate,
                StartTime = ev.StartTime,
                Venue = ev.Venue,
                City = ev.City,
                Province = ev.Province,
                Description = ev.Description,
                DistanceKm = ev.DistanceKm,
                RegistrationCloseDate = ev.RegistrationCloseDate,
                Status = ev.Status,
                OrganiserName = ev.Organiser == null ? string.Empty : $"{ev.Organiser.FirstName} {ev.Organiser.LastName}",
                Categories = ev.Categories.Where(c => isOwner || c.IsActive).Select(c => c.ToDto()).ToList(),
                Route = ev.Route?.ToDto(),
                CurrentForecast = latest?.ToDto()
            };
            return Ok(dto);
        }

        /// <summary>Creates an event owned by the logged-in Organiser.</summary>
        /// <response code="201">The created event.</response>
        /// <response code="400">Invalid data (for example registration closes after the event date).</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpPost]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(EventSummaryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(EventRequestDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dto.EventDate!.Value < today)
                return BadRequest(new { message = "Event date cannot be in the past." });
            if (dto.RegistrationCloseDate!.Value > dto.EventDate.Value)
                return BadRequest(new { message = "Registration must close on or before the event date." });

            var ev = new Event
            {
                OrganiserId = CurrentUserId,
                Name = dto.Name.Trim(),
                EventType = dto.EventType,
                EventDate = dto.EventDate.Value,
                StartTime = dto.StartTime!.Value,
                Venue = dto.Venue.Trim(),
                City = dto.City.Trim(),
                Province = dto.Province.Trim(),
                Description = dto.Description.Trim(),
                DistanceKm = dto.DistanceKm,
                RegistrationCloseDate = dto.RegistrationCloseDate.Value,
                Status = dto.Status ?? "Published"
            };
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { eventId = ev.EventId }, ev.ToSummary());
        }

        /// <summary>Updates an event owned by the logged-in Organiser.</summary>
        /// <response code="200">The updated event.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpPut("{eventId:int}")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(EventSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int eventId, EventRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            if (dto.RegistrationCloseDate!.Value > dto.EventDate!.Value)
                return BadRequest(new { message = "Registration must close on or before the event date." });

            ev.Name = dto.Name.Trim();
            ev.EventType = dto.EventType;
            ev.EventDate = dto.EventDate.Value;
            ev.StartTime = dto.StartTime!.Value;
            ev.Venue = dto.Venue.Trim();
            ev.City = dto.City.Trim();
            ev.Province = dto.Province.Trim();
            ev.Description = dto.Description.Trim();
            ev.DistanceKm = dto.DistanceKm;
            ev.RegistrationCloseDate = dto.RegistrationCloseDate.Value;
            ev.Status = dto.Status ?? ev.Status;
            await _context.SaveChangesAsync();

            return Ok(ev.ToSummary());
        }

        /// <summary>Deletes an event owned by the logged-in Organiser (with its categories, route and forecasts).</summary>
        /// <remarks>An event that has enrolments cannot be deleted - set its status to Cancelled with PUT instead.</remarks>
        /// <response code="204">Deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own this event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">The event has enrolments.</response>
        [HttpDelete("{eventId:int}")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var ev = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Route)
                .Include(e => e.WeatherForecasts)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var hasEnrolments = await _context.Enrolments
                .AnyAsync(en => en.Category != null && en.Category.EventId == eventId);
            if (hasEnrolments)
                return Conflict(new { message = "This event has enrolments and cannot be deleted. Set its status to Cancelled instead." });

            _context.WeatherForecasts.RemoveRange(ev.WeatherForecasts);
            if (ev.Route != null) _context.Routes.Remove(ev.Route);
            _context.EventCategories.RemoveRange(ev.Categories);
            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
