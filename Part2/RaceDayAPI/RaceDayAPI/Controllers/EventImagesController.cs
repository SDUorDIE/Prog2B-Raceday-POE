using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/banner")]
[Authorize(Roles = Roles.Organiser)]
public sealed class EventImagesController(RaceDayDbContext context, ImageStorageService storage) : ApiControllerBase
{
    [HttpPost]
    [RequestSizeLimit(ImageStorageService.MaximumImageBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(int eventId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new { message = "Choose an event banner image." });

        var ev = await context.Events.FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        try
        {
            ev.BannerImageBlobName = await storage.UploadAsync("event-banners", file, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Ok(new { imageUrl = $"/api/images/event-banners/{Uri.EscapeDataString(ev.BannerImageBlobName)}" });
        }
        catch (InvalidImageException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured. Add AzureBlobStorage:ConnectionString to API secrets." });
        }
    }
}
