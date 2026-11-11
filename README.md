# Postgraduate Research (PGR) Student Management System

A modular ASP.NET Core web application demonstrating a **modular monolith** architecture for managing Postgraduate Research (PGR) students, academic supervisory teams, PhD admissions, and student research activities.

---

## Architecture Overview

The application is structured as a **modular monolith** divided into four distinct domain modules, each containing its own controllers, models, data access layer (DAOs/repositories), seeders, and services. Modules communicate across boundaries using lightweight **contracts (interfaces)** rather than referencing each other's data layer directly:

```
PgrStudentManagement.Web/Modules/
├── Students/               # Student records, enrolments, progression
│   └── Contracts/          # IStudentLookup (consumed by Supervisors & Applications)
├── AcademicSupervisors/    # Staff, role eligibility, supervisory teams
│   └── Contracts/          # ISupervisorLookup (consumed by Applications)
├── PhdApplications/        # Prospective PhD proposals, review, admissions lifecycle
└── ResearchActivities/     # Conferences, publications, research documents
```

### 1. Students Module ([`Modules/Students`](./PgrStudentManagement.Web/Modules/Students))
- Handles student records, programme enrolments, mode of study, and progression milestones (thesis titles, expected dates, submissions).
- **Persistence**: Relational SQLite database (`student-management.db`) via Entity Framework Core (`Students` and `Enrolments` tables).
- **Public Contract**: Exposes `IStudentLookup` (returning `StudentSummary`), allowing other modules to verify student existence and full names without coupling to the `Students` persistence schema.

### 2. Academic Supervisors Module ([`Modules/AcademicSupervisors`](./PgrStudentManagement.Web/Modules/AcademicSupervisors))
- Manages supervisor records, role eligibility (Director of Studies, First Supervisor, Second Supervisor, Adviser), and supervisory team assignments.
- Enforces institutional rules (e.g. Director of Studies must be institution-employed, maximum supervisor thresholds, single DoS/First Supervisor per student).
- **Persistence**: Relational SQLite database via EF Core (`AcademicSupervisors`, `AcademicSupervisorEligibleRoles`, `SupervisoryAssignments`).
- **Public Contract**: Exposes `ISupervisorLookup` (returning `SupervisorSummary`), allowing other modules to look up supervisor names and check Main Supervisor eligibility.

### 3. PhD Applications Module ([`Modules/PhdApplications`](./PgrStudentManagement.Web/Modules/PhdApplications))
- **Purpose**: Manages the admissions intake and proposal evaluation pipeline for prospective PhD candidates before they become enrolled students.
- **Domain Model (`PhdApplication`)**:
  - **Identity**: Natural primary key `ApplicationReference` (e.g. `APP-2026-001`).
  - **Applicant Details**: First name, last name, email address.
  - **Proposed Study**: Programme code (e.g. `PHD-COMP`, `PHD-DESIGN`), mode of study (`Full-time` / `Part-time`), proposed cohort start date.
  - **Research Proposal**: Working thesis title and research abstract/summary.
  - **Funding**: Enumerated `FundingType` (`SelfFunded`, `Studentship`, `ResearchCouncil`, `ExternalSponsor`).
  - **Proposed Supervision**: Optional `ProposedSupervisorStaffNumber` foreign key referencing `AcademicSupervisors.StaffNumber`.
  - **Admissions Lifecycle**: `ApplicationStatus` (`Submitted`, `UnderReview`, `InterviewScheduled`, `OfferMade`, `OfferAccepted`, `Rejected`, `Withdrawn`, `Enrolled`).
  - **Decisions & Enrollment**: Submission date, decision timestamp, and optional `EnrolledStudentNumber` foreign key referencing `Students.StudentNumber` once an offer is accepted and the applicant matriculates.
- **Cross-Module Integration**:
  - Uses `ISupervisorLookup` to verify that any proposed supervisor exists and is eligible to act as a Main Supervisor (`DirectorOfStudies` or `FirstSupervisor`).
  - Uses `IStudentLookup` to validate matriculated student records when an application transitions to `Enrolled`.
- **Persistence**: Relational SQLite database via EF Core (`PhdApplications` table).
- **UI / UX & Accessibility Starting Point**: The module's starter views ([`Create.cshtml`](./PgrStudentManagement.Web/Views/PhdApplications/Create.cshtml) and [`EditStatus.cshtml`](./PgrStudentManagement.Web/Views/PhdApplications/EditStatus.cshtml)) are intentionally stripped of form validation and defaulted to plain text inputs, providing a real-world scenario for students to practice modern UI/UX, input types, and accessible form design (see [Lab Resources](#lab-resources-ui--ux-and-accessibility)).

### 4. Research Activities Module ([`Modules/ResearchActivities`](./PgrStudentManagement.Web/Modules/ResearchActivities))
- Tracks student research activities, conference presentations, published papers, and research milestones.
- **Persistence**: Relational SQLite database via EF Core (`ResearchActivities` table).

---

## Database Architecture & Persistence

The application uses **SQLite** for relational domain models with foreign keys.

### 1. SQLite Relational Database (`student-management.db`)
- **Location**: Local file `PgrStudentManagement.Web/student-management.db` (configured in `appsettings.json`).
- **Tables**:
  - `Students`: Core student demographic and research milestone records.
  - `Enrolments`: Programme history and enrolment statuses (`Active`, `Suspended`, `Completed`).
  - `AcademicSupervisors`: Staff records and employment types (`InstitutionEmployed`, `External`).
  - `AcademicSupervisorEligibleRoles`: Many-to-many relationship mapping supervisors to permissible supervisory roles.
  - `SupervisoryAssignments`: Student-supervisor team pairings with start/end dates and role types.
  - `PhdApplications`: Candidate applications, proposals, proposed supervisors, and status history.
  - `ResearchActivities`: Student research activity logs, reflections, supervisory meetings, and conferences with structured metadata.
- **Zero Configuration**: SQLite requires no installation or running services.
- **Automatic Migration & Seeding**: When the application boots, `Program.cs` automatically invokes `context.Database.MigrateAsync()`, ensuring all schema tables are created and seeded if they do not yet exist.
- **Manual Migrations** (optional):
  ```bash
  dotnet ef migrations add <MigrationName> --project PgrStudentManagement.Web
  dotnet ef database update --project PgrStudentManagement.Web
  ```

---

## How to Run the Application

### 1. Build the Solution
From the repository root:
```bash
dotnet build
```

### 2. Run the Web Project
Launch the web application:
```bash
dotnet run --project PgrStudentManagement.Web
```
Or with hot reload:
```bash
dotnet watch --project PgrStudentManagement.Web
```

By default, the application listens on:
- HTTP: `http://localhost:5277`
- HTTPS: `https://localhost:7178`

### 3. Application Startup Sequence
When [PgrStudentManagement.Web/Program.cs](PgrStudentManagement.Web/Program.cs) executes:
1. **Service Registration**: Registers MVC controllers, EF Core SQLite `DbContext`, DAOs, repositories, and cross-module services (`IStudentLookup`, `ISupervisorLookup`).
2. **Database Migration**: Automatically applies pending EF Core migrations (`await context.Database.MigrateAsync()`).
3. **Ordered Data Seeding**:
   - `StudentDataSeeder` (Students)
   - `EnrolmentDataSeeder` (Enrolments referencing `StudentNumber`)
   - `AcademicSupervisorDataSeeder` (Supervisors and supervisory assignments)
   - `PhdApplicationDataSeeder` (PhD applications referencing supervisors and students)
   - `ResearchActivityDataSeeder` (Research activity records)
4. **Endpoint Routing**: Maps controllers and static asset endpoints.

### 4. Running the Tests
To run unit and integration tests across all modules:
```bash
dotnet test
```

---

## Lab Resources: UI / UX and Accessibility

This project includes a two-part hands-on lab sequence focused on improving form and data-table usability and accessibility (WCAG 2.2) in ASP.NET Core MVC, followed by branch-vs-branch usability and accessibility evaluation:

- **Lab 1 — UI/UX & Accessibility Implementation**: [`LabResources/lab1-instructions.md`](./LabResources/lab1-instructions.md)
  - **Workspace & Branch Setup**: Organizing files in a dedicated parent workspace folder so external test suites in Lab 2 can sit outside the repository and evaluate multiple branches (`main` vs `ui-ux-improvements`).
  - **Part 0**: Usability and accessibility audit of the starter forms and table.
  - **Part 1 (Quick Wins)**: Fixing non-WCAG-compliant button colour contrast (`2.12:1` -> `8.86:1`, WCAG 1.4.3) via `:root` CSS variables in `wwwroot/css/site.css` and replacing `<span>` prompts with semantic `<label asp-for="...">` tags (WCAG 1.3.1 & 4.1.2).
  - **Part 2**: Minimising user input to prevent errors by decoupling forms from entities using View Models, auto-generating system fields, and providing post-redirect user feedback via `TempData` and `role="status"` (WCAG 4.1.3).
  - **Part 3**: Selecting appropriate HTML5 input types (dates, emails, radios, selects).
  - **Part 4**: Structuring form layout, visual section grouping (`.form-section`), responsive paired fields (`.form-grid-2`), and proportionate input widths (`.input-width-sm`, `.input-width-md`).
  - **Part 5**: Accessible human-readable display names, hint text (`aria-describedby`), and live character counts (`aria-live="polite"`).
  - **Part 6 & 7**: Server-side validation rules and accessible error presentation (error summary, inline messages, focus management).
  - **Part 8**: Native HTML5 client-side validation (`required`, `maxlength`, `type="email"`, `type="date"`).
  - **Part 9**: Accessible table semantics (`<caption>`, `scope="col"`), status filtering, and screen-reader link context (`Index.cshtml`).
  - **Part 10 (Optional)**: Progressive disclosure and error prevention on status decision forms (`EditStatus.cshtml`).
- **Lab 2 — Branch Comparison Usability & Accessibility Testing (`main` vs. `ui-ux-improvements`)**: [`LabResources/lab2-instructions.md`](./LabResources/lab2-instructions.md)
  - Testing the untouched **`main` branch** against the **`ui-ux-improvements` branch** (either by switching branches with `git switch` or running both concurrently via `git worktree`).
  - **Part 1**: 5-minute Peer Guerrilla Usability Testing with structured task metrics.
  - **Part 2**: Chrome DevTools Accessibility Tree inspection, Lighthouse audits, vision deficiency emulation, and 400% zoom reflow.
  - **Part 3**: Visual keyboard and tab-stop path auditing with **Microsoft Accessibility Insights for Web**.
  - **Part 4**: Automated usability and WCAG 2.2 AA testing with **Playwright** and **axe-core** (`@playwright/test` + `@axe-core/playwright` and .NET xUnit `Microsoft.Playwright` + `Deque.AxeCore.Playwright`).
  - **Part 5**: Branch comparison scorecard and reflection questions.
- **Code Snippets & Heavy Hints Companion**: [`LabResources/code-snippets.md`](./LabResources/code-snippets.md)
  - Concrete C# and Razor implementations for `CreatePhdApplicationViewModel`, `PhdApplicationsController`, `Create.cshtml`, `Details.cshtml`, `Index.cshtml`, and `EditStatus.cshtml`.

---

## Technical Notes

### Asynchronous Pattern in Controllers
When returning view models from asynchronous services in MVC controllers, always `await` the asynchronous task:
```csharp
// Correct:
public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
{
    var supervisors = await _academicSupervisorService.GetAcademicSupervisorsAsync(cancellationToken);
    return View(supervisors);
}
```
Omitting `await` passes an unresolved `Task<...>` rather than the underlying model into Razor's `ViewDataDictionary`, which causes a runtime `System.InvalidOperationException`.
