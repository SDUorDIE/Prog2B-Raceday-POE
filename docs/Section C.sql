;

USE master;
GO

IF DB_ID(N'RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END;
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FirstName NVARCHAR(60) NOT NULL,
    LastName NVARCHAR(60) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(30) NULL,
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Organiser', N'Participant'))
);
GO

CREATE TABLE dbo.Events
(
    EventId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
    OrganiserId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    EventType NVARCHAR(20) NOT NULL,
    EventDate DATE NOT NULL,
    StartTime TIME(0) NOT NULL,
    Venue NVARCHAR(150) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Province NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    RegistrationCloseDate DATE NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Events_Status DEFAULT (N'Published'),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Events_Users_OrganiserId FOREIGN KEY (OrganiserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN (N'Running', N'Walking', N'Cycling')),
    CONSTRAINT CK_Events_Status CHECK (Status IN (N'Draft', N'Published', N'Cancelled', N'Completed')),
    CONSTRAINT CK_Events_RegistrationCloseDate CHECK (RegistrationCloseDate <= EventDate)
);
GO

CREATE TABLE dbo.EventCategories
(
    CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventCategories PRIMARY KEY,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    Capacity INT NOT NULL,
    MinimumAge TINYINT NULL,
    MaximumAge TINYINT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_EventCategories_IsActive DEFAULT (1),
    CONSTRAINT FK_EventCategories_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
    CONSTRAINT UQ_EventCategories_Event_Name UNIQUE (EventId, Name),
    CONSTRAINT CK_EventCategories_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_EventCategories_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT CK_EventCategories_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_EventCategories_AgeRange CHECK (MaximumAge IS NULL OR MinimumAge IS NULL OR MaximumAge >= MinimumAge)
);
GO

CREATE TABLE dbo.Enrolments
(
    EnrolmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Enrolments PRIMARY KEY,
    ParticipantId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME2(0) NOT NULL CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT SYSUTCDATETIME(),
    EmergencyContactName NVARCHAR(120) NOT NULL,
    EmergencyContactPhone NVARCHAR(30) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Enrolments_Status DEFAULT (N'Confirmed'),
    CONSTRAINT FK_Enrolments_Users_ParticipantId FOREIGN KEY (ParticipantId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_EventCategories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.EventCategories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled'))
);
GO

CREATE TABLE dbo.Results
(
    ResultId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Results PRIMARY KEY,
    EnrolmentId INT NOT NULL,
    FinishTime TIME(0) NULL,
    OverallPosition INT NULL,
    CategoryPosition INT NULL,
    Status NVARCHAR(20) NOT NULL,
    RecordedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Results_Enrolments_EnrolmentId FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments(EnrolmentId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Status CHECK (Status IN (N'Finished', N'DNF', N'DNS', N'DSQ')),
    CONSTRAINT CK_Results_Positions CHECK ((OverallPosition IS NULL OR OverallPosition > 0) AND (CategoryPosition IS NULL OR CategoryPosition > 0)),
    CONSTRAINT CK_Results_FinishTime CHECK ((Status = N'Finished' AND FinishTime IS NOT NULL) OR (Status <> N'Finished' AND FinishTime IS NULL))
);
GO

CREATE TABLE dbo.Routes
(
    RouteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Routes PRIMARY KEY,
    EventId INT NOT NULL,
    RouteName NVARCHAR(150) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    ElevationGainM INT NOT NULL CONSTRAINT DF_Routes_ElevationGainM DEFAULT (0),
    StartLocation NVARCHAR(200) NOT NULL,
    FinishLocation NVARCHAR(200) NOT NULL,
    RouteMapUrl NVARCHAR(500) NULL,
    Notes NVARCHAR(1000) NULL,
    CONSTRAINT FK_Routes_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
    CONSTRAINT UQ_Routes_Event UNIQUE (EventId),
    CONSTRAINT CK_Routes_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_Routes_Elevation CHECK (ElevationGainM >= 0)
);
GO

CREATE TABLE dbo.WeatherForecasts
(
    WeatherForecastId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WeatherForecasts PRIMARY KEY,
    EventId INT NOT NULL,
    ForecastDate DATE NOT NULL,
    Condition NVARCHAR(100) NOT NULL,
    TemperatureCelsius DECIMAL(4,1) NOT NULL,
    WindSpeedKph DECIMAL(5,1) NOT NULL,
    PrecipitationChance TINYINT NOT NULL,
    Source NVARCHAR(150) NOT NULL,
    RetrievedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WeatherForecasts_RetrievedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_WeatherForecasts_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
    CONSTRAINT UQ_WeatherForecasts_Event_Date UNIQUE (EventId, ForecastDate),
    CONSTRAINT CK_WeatherForecasts_Wind CHECK (WindSpeedKph >= 0),
    CONSTRAINT CK_WeatherForecasts_Precipitation CHECK (PrecipitationChance BETWEEN 0 AND 100)
);
GO

CREATE INDEX IX_Events_OrganiserId ON dbo.Events(OrganiserId);
CREATE INDEX IX_EventCategories_EventId ON dbo.EventCategories(EventId);
CREATE INDEX IX_Enrolments_ParticipantId ON dbo.Enrolments(ParticipantId);
CREATE INDEX IX_Enrolments_CategoryId ON dbo.Enrolments(CategoryId);
CREATE INDEX IX_WeatherForecasts_EventId ON dbo.WeatherForecasts(EventId);
GO

-- PasswordHash values are illustrative placeholders only. Part 2 must hash passwords securely.
INSERT INTO dbo.Users (FirstName, LastName, Email, PasswordHash, PhoneNumber, Role)
VALUES
    (N'Thabo', N'Mokoena', N'thabo.mokoena@raceday.example', N'placeholder-hash-thabo', N'+27 82 555 0101', N'Organiser'),
    (N'Naledi', N'Jacobs', N'naledi.jacobs@raceday.example', N'placeholder-hash-naledi', N'+27 83 555 0102', N'Organiser'),
    (N'Ayesha', N'Pillay', N'ayesha.pillay@raceday.example', N'placeholder-hash-ayesha', N'+27 84 555 0103', N'Participant'),
    (N'Johan', N'van der Merwe', N'johan.vdm@raceday.example', N'placeholder-hash-johan', N'+27 72 555 0104', N'Participant');
GO

INSERT INTO dbo.Events (OrganiserId, Name, EventType, EventDate, StartTime, Venue, City, Province, Description, RegistrationCloseDate, Status)
VALUES
    (1, N'Jozi Sunrise 10K', N'Running', '2026-11-08', '06:00', N'Zoo Lake Sports Club', N'Johannesburg', N'Gauteng', N'A community 10 km road race through the Zoo Lake precinct.', '2026-11-01', N'Published'),
    (2, N'Cape Peninsula Cycle Challenge', N'Cycling', '2026-12-06', '05:30', N'Green Point Park', N'Cape Town', N'Western Cape', N'A scenic road cycling challenge around the Cape Peninsula.', '2026-11-25', N'Published'),
    (1, N'Durban Beachfront Family Walk', N'Walking', '2027-01-17', '07:00', N'North Beach Amphitheatre', N'Durban', N'KwaZulu-Natal', N'A relaxed, family-friendly fundraising walk along the beachfront.', '2027-01-10', N'Published');
GO

INSERT INTO dbo.EventCategories (EventId, Name, DistanceKm, EntryFee, Capacity, MinimumAge, MaximumAge)
VALUES
    (1, N'10 km Open', 10.00, 180.00, 500, 16, NULL),
    (1, N'5 km Fun Run', 5.00, 100.00, 300, 8, NULL),
    (2, N'100 km Challenge', 100.00, 650.00, 800, 18, NULL),
    (2, N'45 km Social Ride', 45.00, 350.00, 600, 14, NULL),
    (3, N'5 km Family Walk', 5.00, 80.00, 400, NULL, NULL),
    (3, N'10 km Fitness Walk', 10.00, 120.00, 250, 12, NULL);
GO

INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, EnrolmentDate, EmergencyContactName, EmergencyContactPhone, Status)
VALUES
    (3, 1, '2026-09-15 10:15:00', N'Priya Pillay', N'+27 84 555 0199', N'Confirmed'),
    (4, 2, '2026-09-16 14:20:00', N'Anna van der Merwe', N'+27 72 555 0198', N'Confirmed'),
    (3, 3, '2026-09-18 08:45:00', N'Priya Pillay', N'+27 84 555 0199', N'Confirmed'),
    (4, 5, '2026-09-20 09:30:00', N'Anna van der Merwe', N'+27 72 555 0198', N'Pending');
GO

INSERT INTO dbo.Results (EnrolmentId, FinishTime, OverallPosition, CategoryPosition, Status)
VALUES
    (1, '00:52:18', 42, 14, N'Finished'),
    (2, '00:31:06', 88, 31, N'Finished');
GO

INSERT INTO dbo.Routes (EventId, RouteName, DistanceKm, ElevationGainM, StartLocation, FinishLocation, RouteMapUrl, Notes)
VALUES
    (1, N'Zoo Lake 10K Loop', 10.00, 95, N'Zoo Lake Sports Club', N'Zoo Lake Sports Club', N'https://example.com/routes/jozi-sunrise-10k', N'Water point at kilometre 5.'),
    (2, N'Peninsula Coastal Challenge', 100.00, 1150, N'Green Point Park', N'Green Point Park', N'https://example.com/routes/cape-peninsula-100k', N'Wind exposure is expected on coastal sections.'),
    (3, N'Durban Promenade Out-and-Back', 5.00, 18, N'North Beach Amphitheatre', N'North Beach Amphitheatre', N'https://example.com/routes/durban-family-walk', N'Accessible paved route.');
GO

INSERT INTO dbo.WeatherForecasts (EventId, ForecastDate, Condition, TemperatureCelsius, WindSpeedKph, PrecipitationChance, Source)
VALUES
    (1, '2026-11-08', N'Partly cloudy', 18.0, 12.0, 10, N'Weather provider placeholder'),
    (2, '2026-12-06', N'Windy', 21.0, 28.0, 15, N'Weather provider placeholder'),
    (3, '2027-01-17', N'Sunny', 25.0, 10.0, 5, N'Weather provider placeholder');
GO

-- Verification queries: show these result sets in the Part 1 video after a successful execution.
SELECT UserId, FirstName, LastName, Email, Role FROM dbo.Users ORDER BY UserId;
SELECT EventId, Name, EventType, EventDate, City, Province, Status FROM dbo.Events ORDER BY EventId;
SELECT c.CategoryId, e.Name AS EventName, c.Name AS CategoryName, c.DistanceKm, c.EntryFee, c.Capacity
FROM dbo.EventCategories AS c INNER JOIN dbo.Events AS e ON e.EventId = c.EventId ORDER BY e.EventId, c.CategoryId;
SELECT en.EnrolmentId, u.FirstName + N' ' + u.LastName AS Participant, e.Name AS EventName, c.Name AS CategoryName, en.Status
FROM dbo.Enrolments AS en
INNER JOIN dbo.Users AS u ON u.UserId = en.ParticipantId
INNER JOIN dbo.EventCategories AS c ON c.CategoryId = en.CategoryId
INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
ORDER BY en.EnrolmentId;
SELECT r.ResultId, en.EnrolmentId, r.FinishTime, r.OverallPosition, r.CategoryPosition, r.Status FROM dbo.Results AS r INNER JOIN dbo.Enrolments AS en ON en.EnrolmentId = r.EnrolmentId ORDER BY r.ResultId;
SELECT RouteId, EventId, RouteName, DistanceKm, ElevationGainM FROM dbo.Routes ORDER BY EventId;
SELECT WeatherForecastId, EventId, ForecastDate, Condition, TemperatureCelsius, WindSpeedKph, PrecipitationChance FROM dbo.WeatherForecasts ORDER BY EventId;
GO
