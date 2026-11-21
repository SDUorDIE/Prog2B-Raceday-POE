using System.Diagnostics;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;

    public HomeController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? search, string? eventType, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!Request.Query.ContainsKey("from"))
            from = DateOnly.FromDateTime(DateTime.Today);

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(eventType)) query.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (from.HasValue) query.Add($"from={from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        if (to.HasValue) query.Add($"to={to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

        var path = "api/events" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
        var response = await _api.SendAsync<List<EventSummary>>(HttpMethod.Get, path, authenticated: false, cancellationToken: cancellationToken);
        var model = new EventsIndexViewModel
        {
            Search = search,
            EventType = eventType,
            From = from,
            To = to,
            Events = response.Value ?? [],
            ErrorMessage = response.Succeeded ? null : response.ErrorMessage
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var response = await _api.SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{id}", cancellationToken: cancellationToken);
        if (!response.Succeeded && response.StatusCode == HttpStatusCode.NotFound)
            return NotFound();
        if (!response.Succeeded || response.Value is null)
            return View("ApiError", response.ErrorMessage ?? "The event details could not be loaded.");

        return View(response.Value);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
