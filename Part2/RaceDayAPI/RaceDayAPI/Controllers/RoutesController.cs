using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/route")]
    public class RoutesController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public RoutesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Returns the route information for an event.</summary>
        /// <response code="200">The route.</response>
        /// <response code="404">Event or route not found.</response>
        [HttpGet]
        [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoute(int eventId)
        {
            var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null || (ev.Status == "Draft" && !IsOwnerOrganiser(ev.OrganiserId)))
                return NotFound(new { message = "Event not found." });

            var route = await _context.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.EventId == eventId);
            if (route == null) return NotFound(new { message = "This event has no route yet." });

            return Ok(route.ToDto());
        }

        /// <summary>Adds the route to an event owned by the logged-in Organiser.</summary>
        /// <response code="201">The created route.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">The event already has a route.</response>
        [HttpPost]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(RouteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateRoute(int eventId, RouteRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            if (await _context.Routes.AnyAsync(r => r.EventId == eventId))
                return Conflict(new { message = "This event already has a route. Use PUT to update it." });

            var route = new RaceRoute { EventId = eventId };
            Apply(route, dto);
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoute), new { eventId }, route.ToDto());
        }

        /// <summary>Updates the route of an event owned by the logged-in Organiser.</summary>
        /// <response code="200">The updated route.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Event or route not found.</response>
        [HttpPut]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRoute(int eventId, RouteRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var route = await _context.Routes.FirstOrDefaultAsync(r => r.EventId == eventId);
            if (route == null) return NotFound(new { message = "This event has no route yet." });

            Apply(route, dto);
            await _context.SaveChangesAsync();
            return Ok(route.ToDto());
        }

        private static void Apply(RaceRoute route, RouteRequestDto dto)
        {
            route.RouteName = dto.RouteName.Trim();
            route.DistanceKm = dto.DistanceKm;
            route.ElevationGainM = dto.ElevationGainM;
            route.StartLocation = dto.StartLocation.Trim();
            route.FinishLocation = dto.FinishLocation.Trim();
            route.RouteMapUrl = dto.RouteMapUrl;
            route.Notes = dto.Notes;
        }
    }
}
