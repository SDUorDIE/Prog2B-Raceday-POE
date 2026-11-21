using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public sealed class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> MyEvents(CancellationToken cancellationToken)
    {
        if (!IsParticipant()) return RedirectForRole();

        var result = await _api.SendAsync<List<EnrolmentSummary>>(HttpMethod.Get, "api/users/me/enrolments", cancellationToken: cancellationToken);
        if (!result.Succeeded)
            return View("ApiError", result.ErrorMessage ?? "Your events could not be loaded.");

        return View(result.Value ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> EnterEvent(int eventId, CancellationToken cancellationToken)
    {
        if (!IsParticipant()) return RedirectForRole();

        var eventResult = await LoadEventAsync(eventId, cancellationToken);
        if (eventResult is null) return NotFound();
        return View(new EnterEventViewModel { Event = eventResult });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterEvent(int eventId, EnterEventViewModel model, CancellationToken cancellationToken)
    {
        if (!IsParticipant()) return RedirectForRole();
        var eventResult = await LoadEventAsync(eventId, cancellationToken);
        if (eventResult is null) return NotFound();
        model.Event = eventResult;

        if (!eventResult.Categories.Any(c => c.CategoryId == model.Form.CategoryId))
            ModelState.AddModelError("Form.CategoryId", "Choose a category available for this event.");
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.SendAsync<EnrolmentSummary>(HttpMethod.Post, $"api/events/{eventId}/enrolments", new
        {
            model.Form.CategoryId,
            model.Form.EmergencyContactName,
            model.Form.EmergencyContactPhone
        }, cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "You could not be entered in this event.");
            return View(model);
        }

        TempData["StatusMessage"] = "You are entered in the event.";
        return RedirectToAction(nameof(MyEvents));
    }

    [HttpGet]
    public async Task<IActionResult> MyResults(CancellationToken cancellationToken)
    {
        if (!IsParticipant()) return RedirectForRole();

        var result = await _api.SendAsync<List<ResultSummary>>(HttpMethod.Get, "api/users/me/results", cancellationToken: cancellationToken);
        if (!result.Succeeded)
            return View("ApiError", result.ErrorMessage ?? "Your results could not be loaded.");

        return View(result.Value ?? []);
    }

    private async Task<EventDetail?> LoadEventAsync(int eventId, CancellationToken cancellationToken)
    {
        var result = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", cancellationToken: cancellationToken);
        return result.Succeeded ? result.Value : null;
    }

    private bool IsParticipant() => HttpContext.Session.GetString("RaceDay.Role") == "Participant";

    private IActionResult RedirectForRole()
    {
        if (HttpContext.Session.GetString("RaceDay.Role") is null)
            return RedirectToAction("Login", "Auth");

        TempData["StatusMessage"] = "This page is available to Participants only.";
        return RedirectToAction("Index", "Home");
    }
}
