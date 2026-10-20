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
5. Run the project using the green ▶ button.
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

### CI/CD

GitHub Actions automatically restores packages, builds the project, and runs unit tests on every push and pull request.

![RaceDay Part 2 CI passing](docs/part2-ci-green.png)

The workflow file is located at `.github/workflows/part2-ci.yml`.

### Video Presentation

Add your unlisted YouTube demonstration link here before submission:

Video walkthrough: https://youtu.be/tT_cp33TQ88 

### Code Reference


### References
