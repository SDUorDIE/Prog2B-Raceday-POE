using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public sealed class AuthController : Controller
{
    private readonly RaceDayApiClient _api;

    public AuthController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.LoginAsync(model, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to sign in.");
            return View(model);
        }

        HttpContext.Session.SetInt32("RaceDay.UserId", result.Value.UserId);
        HttpContext.Session.SetString("RaceDay.Role", result.Value.Role);
        HttpContext.Session.SetString("RaceDay.Name", $"{result.Value.FirstName} {result.Value.LastName}");
        HttpContext.Session.SetString("RaceDay.Email", result.Value.Email);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (model.Role is not ("Organiser" or "Participant"))
            ModelState.AddModelError(nameof(model.Role), "Choose a valid account type.");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.RegisterAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create the account.");
            return View(model);
        }

        TempData["StatusMessage"] = "Account created. Sign in to continue.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _api.LogoutAsync(cancellationToken);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
