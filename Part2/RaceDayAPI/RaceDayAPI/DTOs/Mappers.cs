using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.DTOs
{
    /// <summary>Converts database entities to the DTOs returned by the API.</summary>
    public static class Mappers
    {
        public static EventSummaryDto ToSummary(this Event e) => new()
        {
            EventId = e.EventId,
            OrganiserId = e.OrganiserId,
            Name = e.Name,
            EventType = e.EventType,
            EventDate = e.EventDate,
            StartTime = e.StartTime,
            Venue = e.Venue,
            City = e.City,
            Province = e.Province,
            Description = e.Description,
            DistanceKm = e.DistanceKm,
            RegistrationCloseDate = e.RegistrationCloseDate,
            Status = e.Status
        };

        public static CategoryDto ToDto(this EventCategory c) => new()
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            Capacity = c.Capacity,
            MinimumAge = c.MinimumAge,
            MaximumAge = c.MaximumAge,
            IsActive = c.IsActive
        };

        public static RouteDto ToDto(this RaceRoute r) => new()
        {
            RouteId = r.RouteId,
            EventId = r.EventId,
            RouteName = r.RouteName,
            DistanceKm = r.DistanceKm,
            ElevationGainM = r.ElevationGainM,
            StartLocation = r.StartLocation,
            FinishLocation = r.FinishLocation,
            RouteMapUrl = r.RouteMapUrl,
            Notes = r.Notes
        };

        public static WeatherDto ToDto(this WeatherForecast w) => new()
        {
            WeatherForecastId = w.WeatherForecastId,
            EventId = w.EventId,
            ForecastDate = w.ForecastDate,
            Condition = w.Condition,
            TemperatureCelsius = w.TemperatureCelsius,
            WindSpeedKph = w.WindSpeedKph,
            PrecipitationChance = w.PrecipitationChance,
            Source = w.Source,
            RetrievedAt = w.RetrievedAt
        };

        // Needs Participant, Category and Category.Event loaded - see WithDetails below.
        public static EnrolmentDto ToDto(this Enrolment e) => new()
        {
            EnrolmentId = e.EnrolmentId,
            EventId = e.Category?.EventId ?? 0,
            EventName = e.Category?.Event?.Name ?? string.Empty,
            CategoryId = e.CategoryId,
            CategoryName = e.Category?.Name ?? string.Empty,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant == null ? string.Empty : $"{e.Participant.FirstName} {e.Participant.LastName}",
            EnrolmentDate = e.EnrolmentDate,
            EmergencyContactName = e.EmergencyContactName,
            EmergencyContactPhone = e.EmergencyContactPhone,
            Status = e.Status
        };

        // Needs Enrolment, Enrolment.Participant, Enrolment.Category and Category.Event loaded.
        public static ResultDto ToDto(this Result r) => new()
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            EventId = r.Enrolment?.Category?.EventId ?? 0,
            EventName = r.Enrolment?.Category?.Event?.Name ?? string.Empty,
            ParticipantName = r.Enrolment?.Participant == null
                ? string.Empty
                : $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}",
            CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
            FinishTime = r.FinishTime,
            OverallPosition = r.OverallPosition,
            CategoryPosition = r.CategoryPosition,
            Status = r.Status,
            RecordedAt = r.RecordedAt
        };

        public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> query) =>
            query.Include(e => e.Participant)
                 .Include(e => e.Category).ThenInclude(c => c!.Event);

        public static IQueryable<Result> WithDetails(this IQueryable<Result> query) =>
            query.Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
                 .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event);
    }
}
