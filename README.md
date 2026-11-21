# Prog2B-Raceday-POE

Sduduzo Zondi
ST10445690
PROG6212 PART 1

# RaceDay

This project is being built progressively across three parts as part of the PROG6212 Portfolio of Evidence:

- **Part 1** (this submission): System planning — ERD, API endpoint plan, and SQL database script.
- **Part 2**: RESTful API in C#, connected to the database, with unit tests and CI/CD.
- **Part 3**: MVC web application consuming the API, with Azure Blob Storage and Docker.

## User Roles

RaceDay supports two distinct user roles:

- **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
- **Participant** — can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

Role-based access is planned at the API level (Part 2) and will be reflected consistently in the MVC interface (Part 3).

## Part 1 Deliverables

All planning documents for this part are in the [`/docs`](./docs) folder:

- `Section A ERD.png` — Entity Relationship Diagram covering the core entities (Users, Events, Categories, Routes, Enrolments, Results), with primary keys, foreign keys, and cardinality marked.
- `Section B Endpoint Plan.pdf` — Full API endpoint plan covering Authentication, User Profile, Events, Categories, Enrolments, and Results.
- `Section C.sql` — SQL Server script that creates the schema matching the ERD and seeds it with sample data (2 Organisers, 2 Participants, 3 Events, categories, routes, and enrolments).

## Setup Instructions

> **Existing RaceDayDB:** do not rerun `docs/Section C.sql` on a database that already contains your work. That script drops and recreates RaceDayDB.

1. Clone this repository.
2. Open SQL Server Management Studio (SSMS).
3. Open `docs/Section C.sql`.
4. Execute the script against your SQL Server instance — it will create the database, all tables, and seed sample data.
5. Review `docs/Section A ERD.png` and `docs/Section B Endpoint Plan.pdf` alongside the script to see how the planning documents map to the schema.

## Part 3: MVC and Deployment

The Part 3 MVC site is in `Part3/RaceDayMVC`. It reads and writes application data only through the Part 2 API; it does not use the API's `DbContext` or connect directly to Azure Storage.

### Local run

1. Install the .NET 10 SDK and make sure the SQL Server instance in the API's `ConnectionStrings:DefaultConnection` is available. The workshop setup uses LocalDB.
2. Configure development-only API secrets from the repository root. Replace the placeholders with your own values; do not commit them:

   ```powershell
   dotnet user-secrets set "OrganiserRegistrationCode" "<your organiser code>" --project Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj
   dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your Azure Storage connection string>" --project Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj
   ```

3. Apply the additive image-reference migration to the same database used by the API:

   ```powershell
   dotnet ef database update --project Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj --startup-project Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj
   ```

4. Start the API and then the MVC site in separate terminals:

   ```powershell
   dotnet run --project Part2/RaceDayAPI/RaceDayAPI/RaceDayAPI.csproj --launch-profile http
   dotnet run --project Part3/RaceDayMVC/RaceDayMVC.csproj --launch-profile http
   ```

The API listens at `http://localhost:5069` and MVC at `http://localhost:5081` by default. Event banners and profile pictures accept JPG, PNG or WebP images up to 5 MB. The API validates images and sends them to private Azure Blob Storage containers; the MVC app uploads through the API, and the API serves the stored images.

### Docker

The MVC multi-stage Dockerfile is `Part3/RaceDayMVC/Dockerfile`. Run the Part 2 API on the host first, then build and run MVC from the repository root:

```powershell
docker build -f Part3/RaceDayMVC/Dockerfile -t raceday-mvc .
docker run --rm -p 8080:8080 -e RaceDayApi__BaseUrl=http://host.docker.internal:5069/ raceday-mvc
```

Open `http://localhost:8080`. The container calls the host API at `host.docker.internal`; the API remains responsible for SQL Server and Azure access.

## CI/CD

A GitHub Actions workflow (`.github/workflows/raceday-build.yml`) restores the API solution, runs the existing API checks, and builds the Part 3 MVC project. The existing documentation workflow continues to check the Part 1 `/docs` files.

**Build status screenshot:**

<!-- Insert your green build screenshot here, e.g.: -->
<!-- ![CI build passing](docs/ci-screenshot.png) -->

## Video Presentation

<!-- Insert your unlisted YouTube link here -->
Video walkthrough: [YouTube link here]

The video covers:
- An overview of the RaceDay system and its two roles.
- ERD design decisions (entity choices, relationships, cardinality).
- API endpoint plan choices (resource structure, role requirements).
- A live run of the SQL script in SSMS.

## Reference List 

GitHub, [s.a.]. Workflow syntax for GitHub Actions. [online] Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax [Accessed 20 September 2026].

The Independent Institute of Education (IIE), 2026. RaceDay: Portfolio of Evidence Part 1 Workshop Slides [PROG6212 Module Material]. The Independent Institute of Education: Unpublished.

Microsoft, 2023. Web API implementation - Azure Architecture Center. [online] Available at: https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-implementation [Accessed 20 September 2026].

Microsoft, 2025. API design - Azure Architecture Center. [online] Available at: https://learn.microsoft.com/en-us/azure/architecture/microservices/design/api-design [Accessed 20 September 2026].

Microsoft, 2026. Primary and foreign key constraints - SQL Server. [online] Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints [Accessed 20 September 2026].
