using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDayAPI.DTOs;

namespace RaceDayAPI.Tests;

public class AuthenticationTests : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _client;
    private readonly RaceDayApiFactory _factory;
    public AuthenticationTests(RaceDayApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterParticipant_WithValidDetails_ReturnsCreated()
    {
        var request = new
        {
            firstName = "Test",
            lastName = "Participant",
            email = $"test{Guid.NewGuid()}@example.com",
            phoneNumber = "0821234567",
            password = "Password123!",
            role = "Participant"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created, but received {(int)response.StatusCode}: {responseBody}");
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var email = $"duplicate{Guid.NewGuid()}@example.com";

        var request = new
        {
            firstName = "Duplicate",
            lastName = "User",
            email,
            phoneNumber = "0821234567",
            password = "Password123!",
            role = "Participant"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk()
    {
        var email = $"login{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        var registerRequest = new
        {
            firstName = "Login",
            lastName = "User",
            email,
            phoneNumber = "0821234567",
            password,
            role = "Participant"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginRequest = new { email, password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrongpassword{Guid.NewGuid()}@example.com";

        var registerRequest = new
        {
            firstName = "Wrong",
            lastName = "Password",
            email,
            phoneNumber = "0821234567",
            password = "Password123!",
            role = "Participant"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginRequest = new { email, password = "WrongPassword123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenLoggedInAsParticipant_ReturnsForbidden()
    {
        var email = $"participant{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        var registerRequest = new
        {
            firstName = "Participant",
            lastName = "Test",
            email,
            phoneNumber = "0821234567",
            password,
            role = "Participant"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var eventRequest = new
        {
            name = "Test 10 km Race",
            eventType = "Running",
            eventDate = "2030-10-15",
            startTime = "07:00:00",
            venue = "Kings Park",
            city = "Durban",
            province = "KwaZulu-Natal",
            description = "A test running event.",
            distanceKm = 10,
            registrationCloseDate = "2030-10-01",
            status = "Published"
        };

        var response = await _client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenLoggedInAsOrganiser_ReturnsCreated()
    {
        var email = $"organiser{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        var registerRequest = new
        {
            firstName = "Organiser",
            lastName = "Test",
            email,
            phoneNumber = "0821234567",
            password,
            role = "Organiser",
            organiserCode = "RACEDAY-ORGANISER-2026"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var eventRequest = new
        {
            name = "Organiser Test Race",
            eventType = "Running",
            eventDate = "2030-10-15",
            startTime = "07:00:00",
            venue = "Kings Park",
            city = "Durban",
            province = "KwaZulu-Natal",
            description = "An organiser-created test event.",
            distanceKm = 10,
            registrationCloseDate = "2030-10-01",
            status = "Published"
        };

        var response = await _client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanEnrolOnce_ButDuplicateEnrolmentReturnsConflict()
    {
        var organiserClient = _factory.CreateClient();
        var organiserEmail = $"organiser{Guid.NewGuid()}@example.com";
        const string organiserPassword = "Password123!";

        var organiserRegister = await organiserClient.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Race",
            lastName = "Organiser",
            email = organiserEmail,
            phoneNumber = "0821234567",
            password = organiserPassword,
            role = "Organiser",
            organiserCode = "RACEDAY-ORGANISER-2026"
        });
        Assert.Equal(HttpStatusCode.Created, organiserRegister.StatusCode);

        var organiserLogin = await organiserClient.PostAsJsonAsync("/api/auth/login",
            new { email = organiserEmail, password = organiserPassword });
        Assert.Equal(HttpStatusCode.OK, organiserLogin.StatusCode);

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            name = "Enrolment Test Race",
            eventType = "Running",
            eventDate = "2030-10-15",
            startTime = "07:00:00",
            venue = "Kings Park",
            city = "Durban",
            province = "KwaZulu-Natal",
            description = "Event used for enrolment testing.",
            distanceKm = 10,
            registrationCloseDate = "2030-10-01",
            status = "Published"
        });
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);

        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventSummaryDto>();
        Assert.NotNull(createdEvent);

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{createdEvent!.EventId}/categories", new
            {
                name = "10 km Senior",
                distanceKm = 10,
                entryFee = 100,
                capacity = 50,
                minimumAge = 18,
                maximumAge = 65
            });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);

        var participantClient = _factory.CreateClient();
        var participantEmail = $"participant{Guid.NewGuid()}@example.com";
        const string participantPassword = "Password123!";

        var participantRegister = await participantClient.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Race",
            lastName = "Participant",
            email = participantEmail,
            phoneNumber = "0821234567",
            password = participantPassword,
            role = "Participant"
        });
        Assert.Equal(HttpStatusCode.Created, participantRegister.StatusCode);

        var participantLogin = await participantClient.PostAsJsonAsync("/api/auth/login",
            new { email = participantEmail, password = participantPassword });
        Assert.Equal(HttpStatusCode.OK, participantLogin.StatusCode);

        var enrolRequest = new
        {
            categoryId = category!.CategoryId,
            emergencyContactName = "Test Contact",
            emergencyContactPhone = "0831234567"
        };

        var firstEnrolment = await participantClient.PostAsJsonAsync(
            $"/api/events/{createdEvent.EventId}/enrolments", enrolRequest);

        var duplicateEnrolment = await participantClient.PostAsJsonAsync(
            $"/api/events/{createdEvent.EventId}/enrolments", enrolRequest);

        Assert.Equal(HttpStatusCode.Created, firstEnrolment.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateEnrolment.StatusCode);
    }
}