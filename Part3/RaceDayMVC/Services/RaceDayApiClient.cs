using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using RaceDayMVC.Models;

namespace RaceDayMVC.Services;

public sealed class RaceDayApiClient
{
    private const string ApiCookieSessionKey = "RaceDay.ApiCookie";
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ApiCallResult<UserProfile>> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default) =>
        SendAsync<UserProfile>(HttpMethod.Post, "api/auth/register", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password,
            model.PhoneNumber,
            model.Role,
            model.OrganiserCode
        }, authenticated: false, cancellationToken);

    public async Task<ApiCallResult<UserProfile>> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        CurrentSession.Clear();
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { model.Email, model.Password })
        };

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var result = await ReadResultAsync<UserProfile>(response, cancellationToken);
            if (!result.Succeeded)
                return result;

            var cookie = response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders)
                ? setCookieHeaders.Select(value => value.Split(';', 2)[0])
                    .FirstOrDefault(value => value.StartsWith("RaceDay.Auth=", StringComparison.Ordinal))
                : null;

            if (string.IsNullOrWhiteSpace(cookie))
                return ApiCallResult<UserProfile>.Failure(HttpStatusCode.BadGateway, "The API did not return an authentication session.");

            CurrentSession.SetString(ApiCookieSessionKey, cookie);
            return result;
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<UserProfile>.Failure(HttpStatusCode.ServiceUnavailable, "The RaceDay API is unavailable. Please try again shortly.");
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Post, "api/auth/logout", authenticated: true);
            using var response = await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The local session still needs to end if the API cannot be reached.
        }
        finally
        {
            CurrentSession.Clear();
        }
    }

    public async Task<ApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string relativeUri,
        object? body = null,
        bool authenticated = true,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(method, relativeUri, authenticated);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            return await ReadResultAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<T>.Failure(HttpStatusCode.ServiceUnavailable, "The RaceDay API is unavailable. Please try again shortly.");
        }
    }

    public string AbsoluteApiUrl(string relativeUri) => new Uri(_http.BaseAddress!, relativeUri.TrimStart('/')).ToString();

    public async Task<ApiCallResult<T>> UploadFileAsync<T>(string relativeUri, IFormFile file, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, relativeUri, authenticated: true);
        var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
            fileContent.Headers.ContentType = contentType;
        form.Add(fileContent, "file", Path.GetFileName(file.FileName));
        request.Content = form;

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            return await ReadResultAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<T>.Failure(HttpStatusCode.ServiceUnavailable, "The RaceDay API is unavailable. Please try again shortly.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri, bool authenticated)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        if (authenticated)
        {
            var cookie = CurrentSession.GetString(ApiCookieSessionKey);
            if (!string.IsNullOrWhiteSpace(cookie))
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }
        return request;
    }

    private static async Task<ApiCallResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
            return ApiCallResult<T>.Success(default);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return ApiCallResult<T>.Failure(response.StatusCode, GetApiMessage(payload));

        if (string.IsNullOrWhiteSpace(payload))
            return ApiCallResult<T>.Success(default);

        try
        {
            var value = JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return ApiCallResult<T>.Success(value);
        }
        catch (JsonException)
        {
            return ApiCallResult<T>.Failure(response.StatusCode, "The API returned a response the application could not read.");
        }
    }

    private static string GetApiMessage(string payload)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString() ?? "The request could not be completed.";
            }
            catch (JsonException)
            {
                // Some API/framework errors aren't JSON; don't expose their raw body in the page.
            }
        }

        return "The request could not be completed. Please review the details and try again.";
    }

    private ISession CurrentSession =>
        _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Session is not available for this request.");
}

public sealed record ApiCallResult<T>(bool Succeeded, HttpStatusCode StatusCode, T? Value, string? ErrorMessage)
{
    public static ApiCallResult<T> Success(T? value) => new(true, HttpStatusCode.OK, value, null);
    public static ApiCallResult<T> Failure(HttpStatusCode statusCode, string errorMessage) => new(false, statusCode, default, errorMessage);
}
