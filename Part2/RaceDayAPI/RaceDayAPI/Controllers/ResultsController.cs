using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    public class ResultsController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Lists the published results of an event, best position first.</summary>
        /// <response code="200">The results.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("api/events/{eventId:int}/results")]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null || (ev.Status == "Draft" && !IsOwnerOrganiser(ev.OrganiserId)))
                return NotFound(new { message = "Event not found." });

            var results = await _context.Results.AsNoTracking().WithDetails()
                .Where(r => r.Enrolment!.Category!.EventId == eventId)
                .OrderBy(r => r.OverallPosition == null)
                .ThenBy(r => r.OverallPosition)
                .ToListAsync();
            return Ok(results.Select(r => r.ToDto()));
        }

        /// <summary>Returns one result, including participant and category. Organiser who owns the event only.</summary>
        /// <response code="200">The result.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Result not found.</response>
        [HttpGet("api/results/{resultId:int}")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResult(int resultId)
        {
            var result = await _context.Results.AsNoTracking().WithDetails()
                .FirstOrDefaultAsync(r => r.ResultId == resultId);
            if (result == null) return NotFound(new { message = "Result not found." });

            var organiserId = result.Enrolment?.Category?.Event?.OrganiserId;
            if (organiserId != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            return Ok(result.ToDto());
        }

        /// <summary>Captures the finish time and positions for an enrolment.</summary>
        /// <response code="201">The created result.</response>
        /// <response code="400">Invalid data (for example Finished without a finish time).</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">A result already exists, or the enrolment was cancelled.</response>
        [HttpPost("api/enrolments/{enrolmentId:int}/result")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateResult(int enrolmentId, ResultRequestDto dto)
        {
            var enrolment = await _context.Enrolments.WithDetails()
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
            if (enrolment.Category?.Event?.OrganiserId != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var problem = Validate(dto);
            if (problem != null) return BadRequest(new { message = problem });

            if (enrolment.Status == "Cancelled")
                return Conflict(new { message = "A cancelled enrolment cannot have a result." });
            if (await _context.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
                return Conflict(new { message = "A result already exists for this enrolment." });

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = dto.FinishTime,
                OverallPosition = dto.OverallPosition,
                CategoryPosition = dto.CategoryPosition,
                Status = dto.Status
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            result.Enrolment = enrolment;   // so the DTO can show participant, category and event
            return CreatedAtAction(nameof(GetResult), new { resultId = result.ResultId }, result.ToDto());
        }

        /// <summary>Corrects an existing result.</summary>
        /// <response code="200">The updated result.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Result not found.</response>
        [HttpPut("api/results/{resultId:int}")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(int resultId, ResultRequestDto dto)
        {
            var result = await _context.Results.WithDetails()
                .FirstOrDefaultAsync(r => r.ResultId == resultId);
            if (result == null) return NotFound(new { message = "Result not found." });
            if (result.Enrolment?.Category?.Event?.OrganiserId != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var problem = Validate(dto);
            if (problem != null) return BadRequest(new { message = problem });

            result.FinishTime = dto.FinishTime;
            result.OverallPosition = dto.OverallPosition;
            result.CategoryPosition = dto.CategoryPosition;
            result.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(result.ToDto());
        }

        // Mirrors the CK_Results_FinishTime rule so the user gets a clear 400 instead of a database error.
        private static string? Validate(ResultRequestDto dto)
        {
            if (dto.Status == "Finished" && dto.FinishTime == null)
                return "FinishTime is required when Status is Finished.";
            if (dto.Status != "Finished" && dto.FinishTime != null)
                return "FinishTime must be empty unless Status is Finished.";
            return null;
        }
    }
}
