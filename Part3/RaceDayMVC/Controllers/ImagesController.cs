using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController(RaceDayApiClient api) : ControllerBase
{
    [HttpGet("event-banners/{**blobName}")]
    public Task<IActionResult> EventBanner(string blobName, CancellationToken cancellationToken) =>
        ProxyImageAsync($"api/images/event-banners/{Uri.EscapeDataString(blobName)}", cancellationToken);

    [HttpGet("profile-pictures/{**blobName}")]
    public Task<IActionResult> ProfilePicture(string blobName, CancellationToken cancellationToken) =>
        ProxyImageAsync($"api/images/profile-pictures/{Uri.EscapeDataString(blobName)}", cancellationToken);

    private async Task<IActionResult> ProxyImageAsync(string apiPath, CancellationToken cancellationToken)
    {
        var result = await api.DownloadImageAsync(apiPath, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            return StatusCode((int)result.StatusCode);
        }

        return File(result.Value.Content, result.Value.ContentType);
    }
}
