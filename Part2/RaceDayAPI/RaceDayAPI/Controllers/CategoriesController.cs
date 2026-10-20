using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    public class CategoriesController : ApiControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>Lists the race categories of an event (age and/or distance based).</summary>
        /// <remarks>Everyone sees active categories; the owning Organiser also sees inactive ones.</remarks>
        /// <response code="200">The categories.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("api/events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null || (ev.Status == "Draft" && !IsOwnerOrganiser(ev.OrganiserId)))
                return NotFound(new { message = "Event not found." });

            var isOwner = IsOwnerOrganiser(ev.OrganiserId);
            var categories = await _context.EventCategories.AsNoTracking()
                .Where(c => c.EventId == eventId && (isOwner || c.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Ok(categories.Select(c => c.ToDto()));
        }

        /// <summary>Returns one category.</summary>
        /// <response code="200">The category.</response>
        /// <response code="404">Category not found.</response>
        [HttpGet("api/categories/{categoryId:int}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int categoryId)
        {
            var category = await _context.EventCategories.AsNoTracking()
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null || category.Event == null ||
                (category.Event.Status == "Draft" && !IsOwnerOrganiser(category.Event.OrganiserId)))
                return NotFound(new { message = "Category not found." });

            return Ok(category.ToDto());
        }

        /// <summary>Adds a category to an event owned by the logged-in Organiser.</summary>
        /// <response code="201">The created category.</response>
        /// <response code="400">Invalid data (for example minimum age above maximum age).</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">The event already has a category with this name.</response>
        [HttpPost("api/events/{eventId:int}/categories")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            if (dto.MinimumAge.HasValue && dto.MaximumAge.HasValue && dto.MinimumAge > dto.MaximumAge)
                return BadRequest(new { message = "MinimumAge cannot be greater than MaximumAge." });

            var name = dto.Name.Trim();
            if (await _context.EventCategories.AnyAsync(c => c.EventId == eventId && c.Name == name))
                return Conflict(new { message = "This event already has a category with that name." });

            var category = new EventCategory
            {
                EventId = eventId,
                Name = name,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                Capacity = dto.Capacity,
                MinimumAge = dto.MinimumAge,
                MaximumAge = dto.MaximumAge
            };
            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { categoryId = category.CategoryId }, category.ToDto());
        }

        /// <summary>Updates a category of an event owned by the logged-in Organiser.</summary>
        /// <response code="200">The updated category.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">Duplicate name, or capacity lower than the current number of enrolments.</response>
        [HttpPut("api/categories/{categoryId:int}")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(int categoryId, CategoryUpdateDto dto)
        {
            var category = await _context.EventCategories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null || category.Event == null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            if (dto.MinimumAge.HasValue && dto.MaximumAge.HasValue && dto.MinimumAge > dto.MaximumAge)
                return BadRequest(new { message = "MinimumAge cannot be greater than MaximumAge." });

            var name = dto.Name.Trim();
            if (await _context.EventCategories.AnyAsync(c =>
                    c.EventId == category.EventId && c.Name == name && c.CategoryId != categoryId))
                return Conflict(new { message = "This event already has a category with that name." });

            var activeEnrolments = await _context.Enrolments
                .CountAsync(e => e.CategoryId == categoryId && e.Status != "Cancelled");
            if (dto.Capacity < activeEnrolments)
                return Conflict(new { message = $"Capacity cannot be lower than the {activeEnrolments} current enrolments." });

            category.Name = name;
            category.DistanceKm = dto.DistanceKm;
            category.EntryFee = dto.EntryFee;
            category.Capacity = dto.Capacity;
            category.MinimumAge = dto.MinimumAge;
            category.MaximumAge = dto.MaximumAge;
            category.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return Ok(category.ToDto());
        }

        /// <summary>Removes a category that has no enrolments.</summary>
        /// <response code="204">Deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">The category has enrolments.</response>
        [HttpDelete("api/categories/{categoryId:int}")]
        [Authorize(Roles = Roles.Organiser)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var category = await _context.EventCategories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null || category.Event == null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            if (await _context.Enrolments.AnyAsync(e => e.CategoryId == categoryId))
                return Conflict(new { message = "This category has enrolments and cannot be removed." });

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
