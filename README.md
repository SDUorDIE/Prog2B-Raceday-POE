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

1. Clone this repository.
2. Open SQL Server Management Studio (SSMS).
3. Open `docs/Section C.sql`.
4. Execute the script against your SQL Server instance — it will create the database, all tables, and seed sample data.
5. Review `docs/Section A ERD.png` and `docs/Section B Endpoint Plan.pdf` alongside the script to see how the planning documents map to the schema.

## CI/CD

A GitHub Actions workflow (`.github/workflows/validate-docs.yml`) runs on every push and validates that the `/docs` folder contains the required planning documents and that `README.md` exists.

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

<!-- Briefly disclose any AI tool use here, per the assignment instructions, e.g.: -->
<!-- AI tools (Claude) were used to assist with planning discussions and drafting the initial ERD structure, endpoint plan format, and SQL script skeleton. All design decisions, entity relationships, and final content were reviewed, adjusted, and understood by the author. -->
