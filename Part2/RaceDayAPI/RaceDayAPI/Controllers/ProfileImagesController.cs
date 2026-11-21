using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/users/me/profile-picture")]
[Authorize]
public sealed class ProfileImagesController(RaceDayDbContext context, ImageStorageService storage) : ApiControllerBase
{
    [HttpPost]
    [RequestSizeLimit(ImageStorageService.MaximumImageBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new { message = "Choose a profile picture." });

        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId, cancellationToken);
        if (user is null) return Unauthorized();

        try
        {
            user.ProfilePictureBlobName = await storage.UploadAsync("profile-pictures", file, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Ok(new { imageUrl = $"/api/images/profile-pictures/{Uri.EscapeDataString(user.ProfilePictureBlobName)}" });
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
