# RaceDay — PROG6212 Portfolio of Evidence

**Part 3: MVC and deployment**

**Author:** Sduduzo Zondi · **Student number:** ST10445690

[![RaceDay Build](https://github.com/SDUorDIE/Prog2B-Raceday-POE/actions/workflows/raceday-build.yml/badge.svg?branch=part3-mvc)](https://github.com/SDUorDIE/Prog2B-Raceday-POE/actions/workflows/raceday-build.yml)

RaceDay is an event-management system developed across the three parts of the PROG6212 Portfolio of Evidence. Part 3 adds an ASP.NET Core MVC website to the existing Part 2 API. The API remains the only application layer that accesses SQL Server and Azure Blob Storage.

## Parts and repository layout

| Part | Contents |
|---|---|
| Part 1 | Planning documents: ERD, endpoint plan, and SQL script in `docs/`. |
| Part 2 | ASP.NET Core Web API, Entity Framework Core, SQL Server, cookie authentication, migrations, and API tests in `Part2/RaceDayAPI/`. |
| Part 3 | ASP.NET Core MVC site, API client, role-based pages, and Dockerfile in `Part3/RaceDayMVC/`. |

```text
.github/workflows/                 GitHub Actions workflows
docs/                              Part 1 planning and database files
Part2/RaceDayAPI/RaceDayAPI/       Part 2 API
Part2/RaceDayAPI/RaceDayAPI.Tests/ API tests
Part2/RaceDayAPI/RaceDayAPI.slnx   API and test solution
Part3/RaceDayMVC/                  Part 3 MVC website and Dockerfile
```

## Architecture

```text
Browser → Part 3 MVC → Part 2 API → SQL Server
                               └──→ Azure Blob Storage
```

MVC sends application requests through `RaceDayApiClient`. MVC does not use the API's `DbContext`, connect directly to SQL Server, or upload directly to Azure. The API enforces authentication and role checks, stores images in private Blob containers, and serves images back through API endpoints. Organiser and Azure settings belong in API User Secrets or the deployment environment; do not put credentials in source files or browser code.

## Part 3 features

- **Authentication and navigation:** participant and organiser registration, login/logout, session-based navigation, and role checks.
- **Organiser workspace:** dashboard totals and upcoming events; event create, edit, and delete; event categories; participant enrolments and statuses; result capture and publication.
- **Participant workspace:** event search and date/activity filters; event details and categories; event entry; My Events with entry status; My Results with date, category, finish time, and positions; profile editing and picture upload.
- **Images:** event banners and profile pictures are uploaded by MVC through the API. The API accepts JPG, PNG, and WebP images up to 5 MB and serves images from private Blob containers.
- **Interface:** responsive Bootstrap layout, role-aware navigation, labelled forms, validation messages, status feedback, and friendly API error pages.

### UI colour balance

The interface follows the 60–30–10 colour guideline using three core colours: light neutral `#F2F5F6` for roughly 60% of the page, deep navy `#17364B` for roughly 30% of the structure and text, and terracotta `#B54420` for roughly 10% of accents and primary actions. Transparency is used for focus rings and shadows without introducing extra palette colours.

## Requirements

- .NET 10 SDK
- SQL Server LocalDB (included with Visual Studio) or a compatible SQL Server
- Docker Desktop with its WSL 2 engine for the Docker steps
- An Azure Storage account for image uploads
- An organiser registration code for creating organiser accounts

## Configure the API

Run these commands from the repository root in PowerShell. Replace each placeholder locally. Never commit the actual values.

```powershell
dotnet user-secrets set "OrganiserRegistrationCode" "<your-organiser-code>" --project "Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj"
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your-Azure-Storage-connection-string>" --project "Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj"
```

The API's default `ConnectionStrings:DefaultConnection` points to `(localdb)\MSSQLLocalDB`. To use another database, configure that connection string locally through User Secrets or an environment variable. The MVC site's `RaceDayApi:BaseUrl` defaults to `http://localhost:5069/`.

## Prepare the database

For this application, use the EF Core migrations on a fresh or migration-managed database. Install the EF tool if needed, then run:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
dotnet ef database update --project "Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj" --startup-project "Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj"
```

This applies the initial schema and the additive image-reference migration.

`docs/Section C.sql` is the Part 1 schema-and-seed script. It drops and recreates `RaceDayDB`; do not run it on a database that contains work. Do not run EF migrations over a database created by that script: its tables already exist and EF's initial migration will attempt to create them again. Use a separate database if you need to demonstrate the SQL script.

## Run locally

Start the API in one terminal:

```powershell
dotnet run --project "Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj" --launch-profile http
```

Start MVC in a second terminal:

```powershell
dotnet run --project "Part3/RaceDayMVC/RaceDayMVC.csproj" --launch-profile http
```

- MVC website: `http://localhost:5081`
- API Swagger: `http://localhost:5069/swagger`

Register a participant through the website. To register an organiser, enter the same organiser code configured in API User Secrets.

## Build and test

From the repository root:

```powershell
dotnet restore "Part2/RaceDayAPI/RaceDayAPI.slnx"
dotnet test "Part2/RaceDayAPI/RaceDayAPI.Tests/RaceDayAPI.Tests.csproj" --no-restore
dotnet build "Part3/RaceDayMVC/RaceDayMVC.csproj" --no-restore
```

## Docker demo

The image contains the MVC site only. Keep the API running on the Windows host; it remains responsible for SQL Server and Azure access. For the container to reach the host API through `host.docker.internal`, run the API bound to all host interfaces in a separate PowerShell terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5069"
dotnet run --no-launch-profile --project "Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj"
```

Then build and run MVC from the repository root in another terminal:

```powershell
docker build -f "Part3/RaceDayMVC/Dockerfile" -t raceday-mvc .
docker run --rm --name raceday-mvc -p 8080:8080 -e "RaceDayApi__BaseUrl=http://host.docker.internal:5069/" -v raceday-mvc-keys:/root/.aspnet/DataProtection-Keys raceday-mvc
```

Open `http://localhost:8080`. The named volume preserves MVC Data Protection keys between container runs. For local demonstration only, the keys are not encrypted at rest; use a protected persistent key store for a deployed application. The Azure connection string stays with the API and is not passed to the MVC container.

## GitHub Actions and submission evidence

The `RaceDay Build` workflow restores the API solution, runs API tests, and builds MVC. It runs for pushes to `main`, `api-part2`, `part3-mvc`, and `part3/**`, and for pull requests to `main`. The latest run on `part3-mvc` passed.

The workshop asks for at least 20 meaningful commits, a green CI screenshot, and an unlisted YouTube video demonstrating MVC, Blob Storage, and Docker. The `part3-mvc` branch has an incremental implementation history. Before final submission:

- Add a screenshot of the successful GitHub Actions run to `docs/raceday-build-passed.png` and link it here.
- Replace the video line below with the unlisted video URL after recording the demo.
- Submit the repository link through ARC; do not submit a ZIP file.

**CI run screenshot:** pending final evidence capture.

**Unlisted demo video:** pending recording.

### Suggested demo sequence

1. Explain the MVC → API → SQL Server/Blob Storage architecture.
2. Show participant and organiser login and role-aware navigation.
3. Show organiser dashboard, event management, categories, enrolments, and results.
4. Show participant event search, details, event entry, My Events, My Results, and profile.
5. Upload and display an event banner and profile picture.
6. Show that a participant is denied access to organiser pages.
7. Run the Dockerized MVC site and show it retrieving event data through the API.
8. Show the successful GitHub Actions run.

## Part 1 planning files

- `docs/Section A ERD.png` — entity relationship diagram.
- `docs/Section B Endpoint Plan.pdf` — planned API resources and endpoints.
- `docs/Section C Tables.png` — database table design.
- `docs/Section C.sql` — SQL Server schema and seed script; see the database warning above.

## References

GitHub. [Workflow syntax for GitHub Actions](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax).

The Independent Institute of Education (IIE), 2026. *RaceDay: Portfolio of Evidence Part 3 Workshop Slides* [PROG6212 module material]. Unpublished.

Microsoft. [Web API implementation best practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-implementation).

Microsoft. [API design](https://learn.microsoft.com/en-us/azure/architecture/microservices/design/api-design).

Microsoft. [Primary and foreign key constraints in SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints).
