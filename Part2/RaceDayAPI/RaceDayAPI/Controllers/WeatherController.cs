using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/weather")]
    public class WeatherController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public WeatherController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Returns the latest stored race-day weather forecast for an event.</summary>
        /// <response code="200">The latest forecast.</response>
        /// <response code="404">Event not found, or it has no forecast yet.</response>
        [HttpGet]
        [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestForecast(int eventId)
        {
            var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null || (ev.Status == "Draft" && !IsOwnerOrganiser(ev.OrganiserId)))
                return NotFound(new { message = "Event not found." });

            var forecast = await _context.WeatherForecasts.AsNoTracking()
                .Where(w => w.EventId == eventId)
                .OrderByDescending(w => w.RetrievedAt)
                .ThenByDescending(w => w.ForecastDate)
                .FirstOrDefaultAsync();
            if (forecast == null) return NotFound(new { message = "No forecast is available for this event yet." });

            return Ok(forecast.ToDto());
        }

        /// <summary>Stores a weather forecast for an event owned by the logged-in Organiser.</summary>
        /// <response code="201">The stored forecast.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">A forecast for that date already exists.</response>
        [HttpPost]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateForecast(int eventId, WeatherRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var date = dto.ForecastDate!.Value;
            if (await _context.WeatherForecasts.AnyAsync(w => w.EventId == eventId && w.ForecastDate == date))
                return Conflict(new { message = "A forecast for that date already exists." });

            var forecast = new WeatherForecast
            {
                EventId = eventId,
                ForecastDate = date,
                Condition = dto.Condition.Trim(),
                TemperatureCelsius = dto.TemperatureCelsius,
                WindSpeedKph = dto.WindSpeedKph,
                PrecipitationChance = dto.PrecipitationChance,
                Source = dto.Source.Trim()
            };
            _context.WeatherForecasts.Add(forecast);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLatestForecast), new { eventId }, forecast.ToDto());
        }
    }
}
