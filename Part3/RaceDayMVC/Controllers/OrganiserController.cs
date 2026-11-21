using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public sealed class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!IsOrganiser()) return RedirectForRole();

        var eventsResult = await _api.SendAsync<List<EventSummary>>(HttpMethod.Get, "api/events/mine", cancellationToken: cancellationToken);
        if (!eventsResult.Succeeded)
            return View("ApiError", eventsResult.ErrorMessage ?? "Your dashboard could not be loaded.");

        var model = new OrganiserDashboardViewModel { Events = eventsResult.Value ?? [] };
        foreach (var item in model.Events)
        {
            var entries = await _api.SendAsync<List<EnrolmentSummary>>(HttpMethod.Get,
                $"api/events/{item.EventId}/enrolments", cancellationToken: cancellationToken);
            if (!entries.Succeeded)
                return View("ApiError", entries.ErrorMessage ?? "Event enrolment totals could not be loaded.");
            model.EnrolmentCounts[item.EventId] = entries.Value?.Count(e => e.Status is "Pending" or "Confirmed") ?? 0;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Events(CancellationToken cancellationToken)
    {
        if (!IsOrganiser()) return RedirectForRole();

        var result = await _api.SendAsync<List<EventSummary>>(HttpMethod.Get, "api/events/mine", cancellationToken: cancellationToken);
        if (!result.Succeeded)
            return View("ApiError", result.ErrorMessage ?? "Your events could not be loaded.");

        return View(result.Value ?? []);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        if (!IsOrganiser()) return RedirectForRole();
        return View(new EventRequestViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)), RegistrationCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!IsOrganiser()) return RedirectForRole();
        if (model.EventType is not ("Running" or "Walking" or "Cycling"))
            ModelState.AddModelError(nameof(model.EventType), "Choose Running, Walking or Cycling.");
        if (model.Status is not ("Draft" or "Published" or "Cancelled" or "Completed"))
            ModelState.AddModelError(nameof(model.Status), "Choose a valid event status.");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.SendAsync<EventSummary>(HttpMethod.Post, "api/events", new
        {
            model.Name,
            model.EventType,
            model.EventDate,
            model.StartTime,
            model.Venue,
            model.City,
            model.Province,
            model.Description,
            model.DistanceKm,
            model.RegistrationCloseDate,
            model.Status
        }, cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event could not be created.");
            return View(model);
        }

        if (model.BannerImage is { Length: > 0 } && result.Value is not null)
        {
            var image = await _api.UploadFileAsync<object>($"api/events/{result.Value.EventId}/banner", model.BannerImage, cancellationToken);
            if (!image.Succeeded)
            {
                TempData["ErrorMessage"] = $"Event created, but its banner was not uploaded: {image.ErrorMessage}";
                return RedirectToAction(nameof(EditEvent), new { eventId = result.Value.EventId });
            }
        }

        TempData["StatusMessage"] = "Event created successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int eventId, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        var result = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", cancellationToken: cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return View("ApiError", result.ErrorMessage ?? "Event details could not be loaded.");

        var ev = result.Value;
        return View(new EventRequestViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            EventType = ev.EventType,
            EventDate = ev.EventDate,
            StartTime = ev.StartTime,
            Venue = ev.Venue,
            City = ev.City,
            Province = ev.Province,
            Description = ev.Description,
            DistanceKm = ev.DistanceKm,
            RegistrationCloseDate = ev.RegistrationCloseDate,
            Status = ev.Status,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int eventId, EventRequestViewModel model, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        if (model.EventType is not ("Running" or "Walking" or "Cycling"))
            ModelState.AddModelError(nameof(model.EventType), "Choose Running, Walking or Cycling.");
        if (model.Status is not ("Draft" or "Published" or "Cancelled" or "Completed"))
            ModelState.AddModelError(nameof(model.Status), "Choose a valid event status.");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.SendAsync<EventSummary>(HttpMethod.Put, $"api/events/{eventId}", new
        {
            model.Name,
            model.EventType,
            model.EventDate,
            model.StartTime,
            model.Venue,
            model.City,
            model.Province,
            model.Description,
            model.DistanceKm,
            model.RegistrationCloseDate,
            model.Status
        }, cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event could not be updated.");
            return View(model);
        }

        if (model.BannerImage is { Length: > 0 })
        {
            var image = await _api.UploadFileAsync<object>($"api/events/{eventId}/banner", model.BannerImage, cancellationToken);
            if (!image.Succeeded)
            {
                TempData["ErrorMessage"] = $"Event details were saved, but the banner was not uploaded: {image.ErrorMessage}";
                return RedirectToAction(nameof(EditEvent), new { eventId });
            }
        }

        TempData["StatusMessage"] = "Event updated successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int eventId, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        var result = await _api.SendAsync<object>(HttpMethod.Delete, $"api/events/{eventId}", cancellationToken: cancellationToken);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "The event could not be deleted.";
        else
            TempData["StatusMessage"] = "Event deleted.";

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        var result = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", cancellationToken: cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return View("ApiError", result.ErrorMessage ?? "Event details could not be loaded.");

        return View(new CategoryManagementViewModel { Event = result.Value });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryManagementViewModel model, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        if (model.Form.MinimumAge.HasValue && model.Form.MaximumAge.HasValue && model.Form.MinimumAge > model.Form.MaximumAge)
            ModelState.AddModelError("Form.MaximumAge", "Maximum age must be at least the minimum age.");

        if (!ModelState.IsValid)
        {
            var eventResult = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", cancellationToken: cancellationToken);
            if (!eventResult.Succeeded || eventResult.Value is null)
                return View("ApiError", eventResult.ErrorMessage ?? "Event details could not be loaded.");
            model.Event = eventResult.Value;
            return View("Categories", model);
        }

        var result = await _api.SendAsync<EventCategory>(HttpMethod.Post, $"api/events/{eventId}/categories", new
        {
            model.Form.Name,
            model.Form.DistanceKm,
            model.Form.EntryFee,
            model.Form.Capacity,
            model.Form.MinimumAge,
            model.Form.MaximumAge
        }, cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The category could not be created.");
            var eventResult = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", cancellationToken: cancellationToken);
            if (!eventResult.Succeeded || eventResult.Value is null)
                return View("ApiError", eventResult.ErrorMessage ?? "Event details could not be loaded.");
            model.Event = eventResult.Value;
            return View("Categories", model);
        }

        TempData["StatusMessage"] = "Category added to the event.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int eventId, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        var eventResult = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", cancellationToken: cancellationToken);
        var enrolmentsResult = await _api.SendAsync<List<EnrolmentSummary>>(HttpMethod.Get, $"api/events/{eventId}/enrolments", cancellationToken: cancellationToken);
        var resultsResult = await _api.SendAsync<List<ResultSummary>>(HttpMethod.Get, $"api/events/{eventId}/results", cancellationToken: cancellationToken);

        if (!eventResult.Succeeded || eventResult.Value is null)
            return View("ApiError", eventResult.ErrorMessage ?? "Event details could not be loaded.");
        if (!enrolmentsResult.Succeeded)
            return View("ApiError", enrolmentsResult.ErrorMessage ?? "Event enrolments could not be loaded.");
        if (!resultsResult.Succeeded)
            return View("ApiError", resultsResult.ErrorMessage ?? "Event results could not be loaded.");

        return View(new EventResultsViewModel
        {
            Event = eventResult.Value,
            Enrolments = enrolmentsResult.Value ?? [],
            Results = resultsResult.Value ?? []
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordResult(int eventId, int enrolmentId, ResultRequestViewModel model, CancellationToken cancellationToken)
    {
        var access = await EnsureOwnedEventAsync(eventId, cancellationToken);
        if (access is not null) return access;

        if (model.Status is not ("Finished" or "DNF" or "DNS" or "DSQ"))
            ModelState.AddModelError(nameof(model.Status), "Choose Finished, DNF, DNS or DSQ.");
        if (model.Status == "Finished" && model.FinishTime is null)
            ModelState.AddModelError(nameof(model.FinishTime), "Enter a finish time for a participant who finished.");
        if (model.Status != "Finished" && model.FinishTime is not null)
            ModelState.AddModelError(nameof(model.FinishTime), "A finish time is only used when status is Finished.");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                ?? "Check the result details and try again.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await _api.SendAsync<ResultSummary>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", new
        {
            model.FinishTime,
            model.OverallPosition,
            model.CategoryPosition,
            model.Status
        }, cancellationToken: cancellationToken);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "The result could not be recorded.";
        else
            TempData["StatusMessage"] = "Result recorded and published.";

        return RedirectToAction(nameof(Results), new { eventId });
    }

    private async Task<IActionResult?> EnsureOwnedEventAsync(int eventId, CancellationToken cancellationToken)
    {
        if (!IsOrganiser())
            return RedirectForRole();

        var result = await _api.SendAsync<List<EventSummary>>(HttpMethod.Get, "api/events/mine", cancellationToken: cancellationToken);
        if (!result.Succeeded)
            return View("ApiError", result.ErrorMessage ?? "Your events could not be loaded.");
        if (result.Value is null || !result.Value.Any(e => e.EventId == eventId))
            return NotFound();

        return null;
    }

    private bool IsOrganiser() => HttpContext.Session.GetString("RaceDay.Role") == "Organiser";

    private IActionResult RedirectForRole()
    {
        if (HttpContext.Session.GetString("RaceDay.Role") is null)
            return RedirectToAction("Login", "Auth");

        TempData["StatusMessage"] = "This page is available to Organisers only.";
        return RedirectToAction("Index", "Home");
    }
}
