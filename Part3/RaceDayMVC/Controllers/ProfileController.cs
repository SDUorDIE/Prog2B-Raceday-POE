using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public sealed class ProfileController : Controller
{
    private readonly RaceDayApiClient _api;

    public ProfileController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!IsSignedIn()) return RedirectToAction("Login", "Auth");

        var result = await _api.SendAsync<UserProfile>(HttpMethod.Get, "api/users/me", cancellationToken: cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return View("ApiError", result.ErrorMessage ?? "Your profile could not be loaded.");

        return View(new ProfileViewModel
        {
            UserId = result.Value.UserId,
            Email = result.Value.Email,
            Role = result.Value.Role,
            ProfilePictureUrl = result.Value.ProfilePictureUrl,
            FirstName = result.Value.FirstName,
            LastName = result.Value.LastName,
            PhoneNumber = result.Value.PhoneNumber
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!IsSignedIn()) return RedirectToAction("Login", "Auth");
        if (!ModelState.IsValid) return View(model);

        var result = await _api.SendAsync<UserProfile>(HttpMethod.Put, "api/users/me", new
        {
            model.FirstName,
            model.LastName,
            model.PhoneNumber
        }, cancellationToken: cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Your profile could not be updated.");
            return View(model);
        }

        HttpContext.Session.SetString("RaceDay.Name", $"{result.Value.FirstName} {result.Value.LastName}");
        TempData["StatusMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!IsSignedIn()) return RedirectToAction("Login", "Auth");
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Choose a JPG, PNG or WebP picture to upload.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadFileAsync<object>("api/users/me/profile-picture", file, cancellationToken);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "The profile picture could not be uploaded.";
        else
            TempData["StatusMessage"] = "Profile picture uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsSignedIn() => HttpContext.Session.GetInt32("RaceDay.UserId").HasValue;
}
