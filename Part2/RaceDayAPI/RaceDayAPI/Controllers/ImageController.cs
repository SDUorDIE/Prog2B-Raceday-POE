using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImageController(ImageStorageService storage) : ControllerBase
{
    [AllowAnonymous, HttpGet("event-banners/{**blobName}")]
    public Task<IActionResult> EventBanner(string blobName, CancellationToken cancellationToken) =>
        GetImageAsync("event-banners", blobName, cancellationToken);

    [AllowAnonymous, HttpGet("profile-pictures/{**blobName}")]
    public Task<IActionResult> ProfilePicture(string blobName, CancellationToken cancellationToken) =>
        GetImageAsync("profile-pictures", blobName, cancellationToken);

    private async Task<IActionResult> GetImageAsync(string container, string blobName, CancellationToken cancellationToken)
    {
        try
        {
            var image = await storage.DownloadAsync(container, blobName, cancellationToken);
            return image is null ? NotFound() : File(image.Value.Content, image.Value.ContentType);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured." });
        }
    }
}
