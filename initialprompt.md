# SAMVAD DMS — Agent Initial Prompt

You are a senior .NET enterprise developer. Your task is to build the **SAMVAD Disaster Management System (DMS)** — a multi-district disaster intake and response coordination platform — described in full in `prd.md`.

You must build it **100% according to the Enterprise Application Development Framework** (`enterprise_framework_doc.md`). Every architectural decision, every file, every pattern must follow that framework without exception. Do not deviate, simplify, or substitute any framework convention.

Read both documents fully before writing a single line of code.

---

## Step 0 — Before You Write Any Code

1. Read `prd.md` completely. Understand all roles, flows, screens, and business rules.
2. Read `enterprise_framework_doc.md` completely. Internalize every standard, pattern, and constraint.
3. Open the Stitch design project: `https://stitch.withgoogle.com/projects/6344629890766436720`
4. Review all files in the `/prototype` folder in the repository.
5. Extract the full design system from Stitch and `/prototype`: color palette, typography scale, spacing, component patterns (buttons, inputs, cards, badges, tables, navigation), and iconography. These are **binding** — every UI you build must match this design system exactly.
6. Only after completing steps 1–5, begin scaffolding.

---

## Solution Structure

Name the solution `SAMVAD.DMS`. Follow the Clean Architecture structure from the framework exactly:

```
SAMVAD.DMS.sln
├── SAMVAD.DMS.Domain
├── SAMVAD.DMS.Application
├── SAMVAD.DMS.Infrastructure
├── SAMVAD.DMS.Api
├── SAMVAD.DMS.Web
├── SAMVAD.DMS.Shared
└── SAMVAD.DMS.External
```

---

## Framework Rules — Non-Negotiable

These are hard constraints from `enterprise_framework_doc.md`. Violating any of these is not acceptable:

**Architecture**
- Clean Architecture with strict layer separation. Each project depends only on what the framework permits.
- **NO Repository Pattern.** Use Services directly — `IUnitOfWork` for data access within services only.
- **NO MediatR.** Use direct service injection everywhere.
- **NO AutoMapper.** All entity-to-DTO mapping must be done manually, property by property.
- **NO ViewModels or Models in the Web project.** Use DTOs from `SAMVAD.DMS.Application` or `SAMVAD.DMS.Shared` only.

**Authentication & Authorization**
- **API project**: ASP.NET Core Identity + JWT Bearer authentication.
- **Web project**: Cookie authentication only. JWT token stored in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie after login. Web controllers never call Identity directly — they call the API via `HttpService`.
- Role-based authorization using `[Authorize(Roles = "...")]` or policies. Define policies in `Program.cs`.

**Data Access**
- Entity Framework Core with `ApplicationDbContext : IdentityDbContext<ApplicationUser>`.
- All entity configurations in `IEntityTypeConfiguration<T>` classes under `Infrastructure/Data/Configurations/`.
- Database: `SULOCHANNEW\\SQLEXPRESS`, username `sa`, password `sa_123`. Connection string in `appsettings.json`.
- Use EF Core migrations. Never modify the database schema manually.

**Web Layer**
- MVC Controller pattern throughout. All Web controllers use `IHttpService` to communicate with the API — never call Application services or DbContext directly from Web.
- **All forms (add, edit, delete, view, list, paging) must use Partial Views and async Ajax patterns.** No full-page reloads for forms.
- Use the Split Pane layout for all admin list + form screens: main content panel with table/list on the left, side pane that opens dynamically on the right for add/edit/view forms.
- All CSS and JS must be in **external files** under `wwwroot/css/` and `wwwroot/js/`. No inline styles. No inline scripts. Use `@section Styles` and `@section Scripts` in views.
- JavaScript organized using the **Module Pattern** (`const moduleName = (function() { ... })()`).
- jQuery Ajax for all dynamic content loading. Toastr for all notifications (success, error, warning).
- jQuery Unobtrusive Validation for client-side form validation.

**API Layer**
- RESTful controllers with `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`.
- All API responses wrapped in `ApiResponseDto<T>` — `{ Success, Message, Data }`.
- All services return `Result<T>` — never throw exceptions for business logic failures.
- Global exception handler middleware in `Api/Program.cs`.

**Shared**
- `Result<T>`, `PaginatedResult<T>`, `ApiResponseDto<T>` defined in `SAMVAD.DMS.Shared`.
- All DTOs in `SAMVAD.DMS.Application/DTOs/` organized by feature folder.

**Validation**
- Server-side: Data Annotations on DTOs (`[Required]`, `[StringLength]`, `[RegularExpression]`, etc.). Check `ModelState.IsValid` in every POST controller action.
- Client-side: jQuery Unobtrusive Validation wired to Data Annotation attributes.

**Security**
- CSRF protection on all Web POST actions: `@Html.AntiForgeryToken()` in forms, `[ValidateAntiForgeryToken]` on POST actions.
- XSS: Use `@Model.Property` (Razor auto-encodes). Never use `Html.Raw()` on user input. In JavaScript, always encode user data with `$('<div>').text(val).html()`.
- SQL Injection: Use EF Core parameterized queries exclusively.
- Content Security Policy header in `Api/Program.cs`.
- Rate limiting on the API: 100 requests/minute fixed window.

**Audit Logging**
- Every create, update, and status change operation must call `IAuditService.LogAuditAsync(userId, action, entityType, entityId, oldValues, newValues)`.
- `AuditLog` entity stored in the database, never deleted.

**Error Handling**
- API: Global exception middleware returns `ApiResponseDto<object>` with `Success: false` and a safe message.
- Web: `app.UseExceptionHandler("/Home/Error")`. Custom Error view.
- All controller actions wrapped in try-catch. Log errors with `ILogger`.

**Tech Stack**
- .NET 8, ASP.NET Core MVC, ASP.NET Core Web API, Entity Framework Core, ASP.NET Core Identity, SQL Server.
- Frontend: Tailwind CSS, jQuery, Font Awesome, Toastr, jQuery Validation.
- No other frontend frameworks (no React, Vue, Angular, Bootstrap).

---

## Domain Entities to Build

Model these entities in `SAMVAD.DMS.Domain/Entities/`. Follow the `ApplicationUser` and `Document` patterns from the framework exactly.

### ApplicationUser *(extends IdentityUser)*
```
FullName, RoleLabel, AssignedDistrictIds (JSON or junction table),
IsActive, CreatedOn, LastLogin
```

### District
```
Id, Name, Code (slug, unique), State, IsActive, CreatedOn
PublicReportUrl (derived: /report/{Code})
```

### Incident
```
Id, IncidentId (GNK-YYYYMMDD-XXXX format), DistrictId,
DisasterType (enum), Priority (enum: Emergency/Standard),
LocationGpsLat, LocationGpsLng, LocationText,
ReporterMobile, Status (enum: Open/InProgress/Closed),
ResolutionNote, TrackingToken (non-guessable UUID),
CreatedAt, UpdatedAt, ClosedAt
Navigation: District, StatusHistory, Comments, MediaFiles
```

### IncidentStatusHistory *(append-only)*
```
Id, IncidentId, FromStatus, ToStatus,
ChangedById, ChangedByName (snapshot), Note, Timestamp
```

### IncidentComment *(append-only)*
```
Id, IncidentId, AuthorId, AuthorName (snapshot),
Body, CreatedAt
```

### IncidentMedia
```
Id, IncidentId, FileName, FilePath, MediaType (Photo/Video),
UploadedBy (Citizen/Admin), UploadedAt
```

### AuditLog *(from framework)*
```
Id, UserId, Action, EntityType, EntityId,
OldValues, NewValues, Timestamp
```

---

## Roles & Authorization Policies

Define these roles in `SeedData.cs` and seed them on startup:

| Role | Name |
|---|---|
| Super Admin | `SuperAdmin` |
| District Admin | `DistrictAdmin` |

Define these authorization policies in `Api/Program.cs` and `Web/Program.cs`:

```csharp
options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
options.AddPolicy("AdminAccess", policy => policy.RequireRole("SuperAdmin", "DistrictAdmin"));
```

District data isolation — enforce in every service method:
- When the caller is `DistrictAdmin`, always filter queries by their assigned district IDs. Never return data from outside their assignment.
- When the caller is `SuperAdmin`, no district filter is applied unless a specific district context is passed.

---

## API Controllers to Build

Each controller in `SAMVAD.DMS.Api/Controllers/`. Follow the framework controller pattern exactly.

### `AuthenticationController`
- `POST /api/authentication/login` — email + password → JWT token + roles + assigned districts
- `POST /api/authentication/forgot-password` — send reset link email
- `POST /api/authentication/reset-password` — validate token, set new password

### `DistrictsController` *(SuperAdmin only)*
- `GET /api/districts` — list all districts (paginated)
- `GET /api/districts/{id}` — get district by ID
- `POST /api/districts` — create district
- `PUT /api/districts/{id}` — update district
- `PATCH /api/districts/{id}/toggle-status` — activate/deactivate

### `IncidentsController`
- `POST /api/incidents/public` — **no auth** — citizen incident submission
- `GET /api/incidents/track/{trackingToken}` — **no auth** — citizen status tracking
- `POST /api/incidents/list` — paginated list with filters (DistrictAdmin scoped, SuperAdmin unscoped)
- `GET /api/incidents/{id}` — full incident detail
- `PATCH /api/incidents/{id}/status` — update status (mandatory note for CLOSED)
- `POST /api/incidents/{id}/comments` — add comment
- `POST /api/incidents/{id}/media` — upload admin evidence (multipart)
- `GET /api/incidents/{id}/export-pdf` — generate PDF report
- `POST /api/incidents/export-excel` — bulk Excel export with filters

### `UsersController` *(SuperAdmin only)*
- `POST /api/users/list` — paginated admin user list
- `GET /api/users/{id}` — get user detail
- `POST /api/users` — create District Admin account (sends set-password email)
- `PUT /api/users/{id}` — update user (name, districts, role label)
- `PATCH /api/users/{id}/toggle-status` — activate/deactivate
- `POST /api/users/{id}/reset-password` — trigger reset email

### `DashboardController`
- `GET /api/dashboard/analytics?districtId=` — KPI cards + chart data
- `GET /api/dashboard/cross-district` — *(SuperAdmin only)* all-district aggregated data

### `NotificationsController` *(internal)*
- Triggered internally by `IncidentService` on status changes — not called by Web directly.

---

## Web Controllers & Views to Build

Each Web controller in `SAMVAD.DMS.Web/Controllers/`. Use `IHttpService` for all API calls. Follow the MVC + partial view + jQuery Ajax pattern exactly.

### `AccountController` — Auth screens (no shell)
- `GET/POST /Account/Login` → full view, cookie auth
- `GET/POST /Account/ForgotPassword` → full view
- `GET/POST /Account/ResetPassword` → full view (token from query string)

### `PublicController` — Public screens (no shell, no auth)
- `GET /report/{districtCode}` → Public Reporting Form (full view, no layout shell)
- `POST /report/{districtCode}` → submit incident, return success view
- `GET /track/{trackingToken}` → Status Tracking View (full view, no layout shell)

### `DashboardController` — Analytics Home
- `GET /Dashboard` → shell layout, returns view (data loaded via Ajax on page load)
- `POST /Dashboard/Analytics` → partial — returns KPI cards + chart data as JSON
- `POST /Dashboard/LiveFeed` → partial — paginated incident feed with filters

### `IncidentsController` — All Incidents
- `GET /Incidents` → shell layout, split-pane list view
- `POST /Incidents/List` → partial — paginated/filtered incident table
- `GET /Incidents/Detail/{id}` → side pane partial — full ticket detail
- `POST /Incidents/UpdateStatus/{id}` → Ajax — status change with note
- `POST /Incidents/AddComment/{id}` → Ajax — post comment
- `GET /Incidents/ExportExcel` → file download
- `GET /Incidents/ExportPdf/{id}` → file download

### `AdminsController` *(SuperAdmin only)* — Manage Admins
- `GET /Admins` → shell layout, split-pane list view
- `POST /Admins/List` → partial — paginated admin table
- `GET /Admins/Add` → partial — add form in side pane
- `POST /Admins/Add` → Ajax — create admin
- `GET /Admins/Edit/{id}` → partial — edit form in side pane
- `POST /Admins/Edit/{id}` → Ajax — update admin
- `POST /Admins/ToggleStatus/{id}` → Ajax
- `POST /Admins/ResetPassword/{id}` → Ajax

### `DistrictsController` *(SuperAdmin only)* — Manage Districts
- `GET /Districts` → shell layout, split-pane list view
- `POST /Districts/List` → partial — paginated district table
- `GET /Districts/Add` → partial — add form
- `POST /Districts/Add` → Ajax — create district
- `GET /Districts/Edit/{id}` → partial — edit form
- `POST /Districts/Edit/{id}` → Ajax — update district
- `POST /Districts/ToggleStatus/{id}` → Ajax

### `SettingsController` — User Settings
- `GET /Settings` → shell layout, profile + password change view
- `POST /Settings/UpdateProfile` → Ajax — partial view
- `POST /Settings/ChangePassword` → Ajax — partial view

---

## Views & Partial Views

Organize views under `SAMVAD.DMS.Web/Views/` following the framework structure. For every admin list screen, use the **Split Pane Layout** from the framework:
- `_Layout.cshtml` — master shell layout with top navigation bar and sidebar
- `_PublicLayout.cshtml` — standalone layout for public and auth screens (no shell)
- For each admin controller: `Index.cshtml` (split pane wrapper) + partials: `_List.cshtml`, `_Add.cshtml`, `_Edit.cshtml`, `_Detail.cshtml`

Shell layout must render:
- Top navigation bar: SAMVAD DMS logo, district selector dropdown (scoped by role), admin profile menu (name, role label, Profile Settings, Change Password, Logout)
- Sidebar: role-conditional navigation links
  - Both roles: Dashboard, All Incidents, Settings
  - SuperAdmin only: Cross-District Overview (within Dashboard), Manage Districts, Manage Admins
- Mobile: sidebar collapses to hamburger menu
- Active district context always visible in top bar; switching district reloads dashboard scope

---

## Design System Implementation

All visual implementation must match the design system extracted from the Stitch project and `/prototype` folder.

- Apply Tailwind CSS utility classes throughout — no custom CSS unless the design requires something Tailwind cannot express
- Use Font Awesome icons for all iconography — match the icon style observed in Stitch
- Color, spacing, typography, and component patterns must match Stitch exactly on screens that are designed there
- For the shell and auth screens (not yet in Stitch): derive and apply the same design tokens — document in code comments which Stitch screens informed the decision
- Public screens (Reporting Form, Success, Status Tracker) and Admin screens must have visually distinct design languages — do not share layout patterns between them
- Toastr for all success/error/warning notifications — position and theme consistent with the design system
- Status badges: OPEN = yellow/amber, IN PROGRESS = blue, CLOSED = green. Priority badges: EMERGENCY = red, STANDARD = gray.

---

## Services to Build in `SAMVAD.DMS.Application/Services/`

Build each service as an interface + implementation pair. Inject `IUnitOfWork` and `IAuditService`. Return `Result<T>` from all methods. Audit every mutating operation.

- `IAuthenticationService` / `AuthenticationService` — login, forgot password, reset password, JWT generation
- `IIncidentService` / `IncidentService` — create (public), list (scoped), get detail, update status, add comment, upload media, generate tracking token, export Excel/PDF
- `IDistrictService` / `DistrictService` — CRUD, toggle status, get by code
- `IUserService` / `UserService` — create admin, list, update, toggle status, trigger password reset email
- `IDashboardService` / `DashboardService` — analytics aggregations, KPI calculations, cross-district overview
- `INotificationService` / `NotificationService` — email dispatch (new incident, status change), SMS dispatch (citizen tracking link, emergency alert)
- `IAuditService` / `AuditService` — log audit, paginated audit query
- `IEmailService` / `EmailService` in Infrastructure — SMTP send implementation
- `ISmsService` / `SmsService` in `SAMVAD.DMS.External` — SMS gateway integration

---

## DTOs to Build in `SAMVAD.DMS.Application/DTOs/`

Organize by feature folder. All DTOs have Data Annotation validation attributes.

**Auth/** — `LoginRequestDto`, `LoginResponseDto`, `ForgotPasswordRequestDto`, `ResetPasswordRequestDto`

**Incident/** — `IncidentPublicSubmitDto`, `IncidentListItemDto`, `IncidentDetailDto`, `IncidentFilterDto`, `UpdateStatusDto`, `AddCommentDto`, `IncidentStatusHistoryDto`, `IncidentCommentDto`, `IncidentMediaDto`, `IncidentTrackingDto` (citizen view)

**District/** — `DistrictRequestDto`, `DistrictResponseDto`, `DistrictListItemDto`

**User/** — `CreateAdminDto`, `UpdateAdminDto`, `AdminListItemDto`, `AdminDetailDto`, `ChangePasswordDto`, `UpdateProfileDto`

**Dashboard/** — `DashboardAnalyticsDto`, `KpiCardDto`, `IncidentDistributionDto`, `StatusBreakdownDto`, `CrossDistrictOverviewDto`

**Audit/** — `AuditLogDto`, `AuditFilterDto`

---

## Key Business Logic Rules

Implement these precisely in the relevant services:

1. **Incident ID generation**: Format `{DISTRICT-CODE}-{YYYYMMDD}-{4-digit sequential counter per district per day}`. E.g. `GNK-20250610-0042`.
2. **Tracking token**: Generate a cryptographically random UUID (`Guid.NewGuid().ToString()`) stored on the incident. Never expose the internal incident ID in the citizen-facing URL.
3. **Status transitions**: Only forward — OPEN → IN PROGRESS → CLOSED. OPEN → CLOSED is allowed with a mandatory reason. Service must reject invalid transitions with `Result.Failure`.
4. **Status history**: Every status change appends an immutable `IncidentStatusHistory` record. Never update or delete these.
5. **Comments**: Append-only. Never update or delete posted comments.
6. **District scoping**: In every `IncidentService` method, if the calling user is `DistrictAdmin`, apply a filter: `WHERE DistrictId IN (user.AssignedDistrictIds)`. If `SuperAdmin`, no filter unless `districtId` param is provided.
7. **Password reset token**: Use ASP.NET Core Identity's `GeneratePasswordResetTokenAsync` and `ResetPasswordAsync`. Token valid for 30 minutes — configure in Identity options.
8. **Account lockout**: Configure Identity lockout: `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`.
9. **Media uploads**: Validate MIME type server-side (not just extension). Store files in a configurable path. Max 5 files per incident, 20MB each.
10. **Audit logging**: Call `AuditService.LogAuditAsync` on: incident creation, status change, comment added, user created/updated/deactivated, district created/updated.

---

## Notifications

Implement in `NotificationService`. Trigger from `IncidentService` — never from controllers.

| Trigger | Channel | Recipient | Content |
|---|---|---|---|
| Incident submitted (public form) | SMS | Reporter mobile | Incident ID + `/track/{token}` URL |
| Incident submitted | Email | All DistrictAdmins for that district | ID, type, priority, location, link to Ticket Detail |
| Status → IN PROGRESS | Email | DistrictAdmins + configured stakeholders | ID, status change, admin name, timestamp, link |
| Status → CLOSED | Email | DistrictAdmins + configured stakeholders | ID, resolution note, closed by, timestamp, link |
| Emergency incident submitted | SMS (optional, configurable) | Configured admin mobiles for that district | Short alert with ID, type, location |

Stakeholder email lists are stored per district. Email failure must be logged — never surface as an error to the user.

---

## PDF & Excel Export

### Excel Export (`IncidentService.ExportExcelAsync`)
- Use a library compatible with .NET 8 (e.g., ClosedXML or EPPlus).
- One row per incident, all key fields as columns.
- Respects active filters. SuperAdmin can export across all districts.
- Filename: `DMS_Export_{DISTRICT}_{YYYYMMDD_HHMMSS}.xlsx`

### PDF Incident Report (`IncidentService.ExportPdfAsync`)
- Use a library compatible with .NET 8 (e.g., QuestPDF or DinkToPdf).
- Sections in order: Header (logo, title, Incident ID, generated-by, District Approval Stamp placeholder), Incident Summary table, Visual Evidence (embedded photos, video thumbnails), Operational Narrative (comment thread + status history timeline), Footer (page numbers, confidential disclaimer).
- Filename: `Incident_Report_{IncidentId}_{YYYYMMDD}.pdf`

---

## Configuration

`appsettings.json` in both `Api` and `Web` projects:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SULOCHANNEW\\SQLEXPRESS;Database=SAMVAD_DMS;User Id=sa;Password=sa_123;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Key": "samvad-dms-super-secret-key-change-in-production",
    "Issuer": "SAMVAD.DMS.Api",
    "Audience": "SAMVAD.DMS.Web",
    "ExpiryHours": 1
  },
  "ApiBaseUrl": "https://localhost:7001",
  "FileStorage": {
    "UploadPath": "uploads/incidents",
    "MaxFileSizeMb": 20,
    "MaxFilesPerIncident": 5
  },
  "Sms": {
    "Enabled": true,
    "GatewayUrl": "",
    "ApiKey": ""
  },
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 587,
    "FromAddress": "noreply@samvad-dms.gov.in",
    "FromName": "SAMVAD DMS"
  }
}
```

---

## Seed Data

In `Infrastructure/Data/SeedData.cs`, seed on application startup:

1. Roles: `SuperAdmin`, `DistrictAdmin`
2. A default Super Admin account: email `superadmin@samvad.gov.in`, password `Admin@12345`, `FullName = "System Administrator"`, `IsActive = true`
3. One sample district: Name `Gangtok`, Code `GNK`, State `Sikkim`, IsActive `true`

---

## Build Order

Build in this order to respect dependency flow:

1. `SAMVAD.DMS.Domain` — entities, enums, interfaces
2. `SAMVAD.DMS.Shared` — `Result<T>`, `PaginatedResult<T>`, `ApiResponseDto<T>`, all DTOs
3. `SAMVAD.DMS.Application` — service interfaces + implementations, `DependencyInjection.cs`
4. `SAMVAD.DMS.Infrastructure` — `ApplicationDbContext`, EF configurations, `UnitOfWork`, `EmailService`, `CurrentUserService`, `SeedData`, migrations, `DependencyInjection.cs`
5. `SAMVAD.DMS.External` — `SmsService`
6. `SAMVAD.DMS.Api` — controllers, `Program.cs` (Identity, JWT, policies, middleware, rate limiting, CORS, global exception handler)
7. `SAMVAD.DMS.Web` — controllers, views, partial views, JS modules, CSS, `Program.cs` (cookie auth, HttpService, HttpClient registration)

---

## Definition of Done

Before marking any feature complete, verify:

- [ ] Service method returns `Result<T>` — never throws for business logic failures
- [ ] Every mutating operation calls `AuditService.LogAuditAsync`
- [ ] API controller wraps response in `ApiResponseDto<T>`
- [ ] All POST API actions validate `ModelState.IsValid`
- [ ] All Web POST actions have `[ValidateAntiForgeryToken]` and forms have `@Html.AntiForgeryToken()`
- [ ] No inline CSS or JS anywhere — all in external files
- [ ] All forms use partial views loaded into the side pane via jQuery Ajax
- [ ] Toastr used for all user notifications (no `alert()`)
- [ ] District scoping enforced in every service method that returns incident or user data
- [ ] Status history and comments are append-only — no update or delete paths exist
- [ ] Design matches Stitch / `/prototype` design system
- [ ] EF migration created and applied for any new entity or schema change

---

*Use `enterprise_framework_doc.md` as your implementation bible throughout. When in doubt about any pattern, naming convention, or structure, refer to that document first.*