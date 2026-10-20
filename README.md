ST10445690
SDUDUZO ZONDI
PROG 2B 
POE

## RACEDAY

## Part 2 — RESTful API

The RaceDay API is built using ASP.NET Core Web API, C#, Entity Framework Core, and SQL Server. It implements the API plan created in Part 1.

### Roles

- **Organiser** — manages events and categories, views event enrolments, and captures race results.
- **Participant** — registers and logs in, views events and categories, enrols in an event, and views personal enrolments and results.

Authentication uses secure cookie-based sessions. Passwords are hashed before being stored, and role-based access prevents users from accessing functions that do not belong to their role.

### Run the API

1. Open `Part2/RaceDayAPI/RaceDayAPI.slnx` in Visual Studio.
2. Check the SQL Server connection string in `RaceDayAPI/appsettings.json`.
3. Apply the Entity Framework Core migrations to create or update the database.
4. Set `RaceDayAPI` as the startup project.
5. Run the project using the green button.
6. Open Swagger in the browser to view and test all API endpoints.

### Run Unit Tests

Open **Test Explorer** in Visual Studio and select **Run All**.

The test suite verifies:

- Successful registration and login.
- Login failure with an incorrect password.
- Protected endpoints reject unauthenticated users.
- Duplicate registration is rejected.
- Participants cannot create organiser-only events.
- Organisers can create events.
- Participants can enrol in an event.
- Duplicate enrolments are rejected.

  <img width="1917" height="1015" alt="image" src="https://github.com/user-attachments/assets/407ac7d0-1439-42d4-a45b-9d79487a2ef6" />


### CI/CD

GitHub Actions automatically restores packages, builds the project, and runs unit tests on every push and pull request.

 <img width="1917" height="1023" alt="image" src="https://github.com/user-attachments/assets/3c75ba8b-a966-4dc8-8fd3-1959422f0b18" />


The workflow file is located at `.github/workflows/part2-ci.yml`.

### Video Presentation

Add your unlisted YouTube demonstration link here before submission:

Video walkthrough: https://youtu.be/tT_cp33TQ88 

### Code Reference

Class code. The cookie authentication setup follows the Authentication_API project taught in class (IIE, 2026): registering a user, hashing the password with PasswordHasher<T>, creating claims at login, signing in with SignInAsync, and protecting endpoints with [Authorize(Roles = ...)]. These parts were adapted as follows:

Area	Taken from the class code	Changed or added for RaceDay
Program.cs	Cookie authentication and authorisation registration	401 and 403 responses instead of redirects, UseAuthentication before UseAuthorization, Swagger with XML comments
AuthController	Register and login flow, password hashing, claims, SignInAsync	Organiser code, user id claim, duplicate-email 409, logout, DTO field named Password
Role checks	[Authorize(Roles = ...)] on role-specific controllers	Ownership checks and Organiser/Participant rules for every RaceDay endpoint

### References
GitHub, [s.a.]. Workflow syntax for GitHub Actions. [online] Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax [Accessed 20 September 2026].

The Independent Institute of Education (IIE), 2026. RaceDay: Portfolio of Evidence Part 1 Workshop Slides [PROG6212 Module Material]. The Independent Institute of Education: Unpublished.

Microsoft, 2023. Web API implementation - Azure Architecture Center. [online] Available at: https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-implementation [Accessed 20 September 2026].

Microsoft, 2025. API design - Azure Architecture Center. [online] Available at: https://learn.microsoft.com/en-us/azure/architecture/microservices/design/api-design [Accessed 20 September 2026].

Microsoft, 2026. Primary and foreign key constraints - SQL Server. [online] Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints [Accessed 20 September 2026].

Microsoft, [s.a.]. Use cookie authentication without ASP.NET Core Identity. [online] Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie [Accessed 18 October 2026].

Microsoft, [s.a.]. Migrations overview - EF Core. [online] Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ [Accessed 18  October 2026].

Microsoft, [s.a.]. Integration tests in ASP.NET Core. [online] Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests [Accessed 18 October 2026].
