# Disaster Management System (DMS)
## Product Requirements Document
### Multi-District | v3.1

---

> ## Agent Instructions
>
> You are building the SAMVAD Disaster Management System (DMS) — a full-stack web application for multi-district disaster intake and response coordination. This PRD is your complete specification. Implement every requirement fully and correctly.
>
> **Design Source of Truth — Google Stitch:**
> All visual implementation must reference the Stitch project and local prototype samples below. Stitch designs define layout, components, color, spacing, typography, and interaction patterns. Where a screen exists in Stitch, match it exactly. Where a screen is not yet in Stitch, apply the same design language, tokens, and component patterns observed across existing frames.
>
> **Stitch Project:** `https://stitch.withgoogle.com/projects/6344629890766436720`
> **Local Design Samples:** `/prototype` folder in the project repository
>
> **Before writing any code**, open the Stitch project and review all files in the `/prototype` folder. Extract and treat the following as binding design tokens for the entire build:
> - Color palette (primary, secondary, surface, background, error, success, warning states)
> - Typography scale (font family, weights, sizes for headings, body, labels, captions)
> - Spacing and layout grid
> - Component patterns (buttons, inputs, cards, chips, badges, modals, tables, navigation)
> - Iconography style
>
> Do not invent visual patterns that conflict with what is observed in Stitch or the `/prototype` folder. The public-facing surfaces (reporting form, status tracker) and the admin dashboard are visually distinct — do not mix their design languages.
>
> **Shell & Auth screens are not yet in Stitch.** Derive their visual design strictly from the existing Stitch project and `/prototype` samples — same tokens, same component patterns. In your implementation notes, document which screens or prototype files from the Stitch project informed each design decision on these screens.
>
> You are building the front-end shell and authentication system from scratch. There is no existing codebase to inherit from.

---

## 1. What This System Does

The DMS is a web-based disaster intake and response coordination platform. It provides:
- A single, standardized public channel for citizens to report disasters in any registered district.
- A unified command dashboard for district administrators to monitor, action, and document incidents within their district.
- A super-admin layer for platform-wide oversight across all districts.

The system is built from scratch — including the front-end shell, navigation, and authentication system.

---

## 2. Roles & Access Model

The system has three distinct roles arranged in a hierarchy.

### Super Admin
- Platform-level administrator. Has access to all districts.
- Can view incidents, analytics, and reports across every district simultaneously.
- Can create and manage District Admin accounts.
- Can register new districts into the platform.
- Cannot be created through the UI — provisioned at the system level.

### District Admin
- Can be assigned to one or multiple districts by the Super Admin.
- Can only see, action, and export incidents belonging to their assigned district(s).
- When assigned to multiple districts, they can switch between their assigned districts using the district selector in the shell header — they never see districts outside their assignment.
- Cannot see other districts' data, users, or analytics.
- Can manage their own profile and password.
- Accounts are created by the Super Admin.

### Citizen
- Has no account and never creates one.
- Submits reports via a public web link (district-specific URL).
- Tracks their report status via a unique link sent to their phone.
- No access to any admin-facing screen or data.

---

## 3. User Flows

### 3.1 Citizen Flow

```
1. Citizen opens district-specific public reporting link
   (e.g., /report/gangtok — no login required)
        ↓
2. Fills out Reporting Form
        ↓
3. Submits → Incident created with status: OPEN
        ↓
4. SMS sent to reporter's mobile:
   Incident ID + unique status tracking link
        ↓
5. Citizen visits tracking link anytime
   to check live status of their report
```

### 3.2 District Admin Flow

```
1. Admin opens the DMS web app
        ↓
2. Enters email + password on Login screen
        ↓
3. Lands on Analytics & Dashboard Home
   (scoped to their assigned district(s))
        ↓
5. Views Live Incident Feed + KPI charts
        ↓
6. Clicks incident → Ticket Detail screen
        ↓
7. Reviews details, media, and location
        ↓
8. Updates status: OPEN → IN PROGRESS → CLOSED
   (each transition auto-logs timestamp + admin name)
        ↓
9. Adds internal comments to the ticket thread
        ↓
10. Generates PDF Incident Report for official records
    OR bulk-exports data to Excel from All Incidents view
```

### 3.3 Super Admin Flow

```
1. Logs in (same login screen, elevated access on auth)
        ↓
2. Lands on Super Admin Dashboard
   — Cross-district analytics (all districts)
   — District selector to drill into a specific district
        ↓
3. Can switch to any district's dashboard view
        ↓
4. Manages Districts: register new districts, edit district details
        ↓
5. Manages Admin Accounts:
   create District Admin accounts, deactivate accounts,
   trigger password resets
```

### 3.4 Authentication Flows

#### Login
```
1. Enter email + password
2. On success → redirect to role-appropriate dashboard landing
3. On failed credentials → inline error message (do not specify which field is wrong)
4. After 5 consecutive failed attempts → account temporarily locked for 15 minutes,
   user shown message to try again later or use Forgot Password
```

#### Forgot Password
```
1. Click "Forgot Password" on login screen
2. Enter registered email address
3. Confirmation message shown on screen (do not indicate if email exists or not)
4. If email is registered: reset link sent, valid for 30 minutes
5. User clicks link → Reset Password screen
6. Enter new password + confirm password (must meet strength requirements)
7. On success → redirect to Login with confirmation banner
```

---

## 4. Front-End Shell

The shell is the persistent UI frame that wraps all admin dashboard screens. It must be built from scratch. **Shell designs are not yet in Stitch** — derive the visual language from the Stitch project (`https://stitch.withgoogle.com/projects/6344629890766436720`) and the `/prototype` folder, and apply it consistently here.

### 4.1 Shell Components

**Top Navigation Bar**
- SAMVAD DMS logo/wordmark (left-aligned)
- District selector — Super Admin: dropdown of all districts; District Admin: dropdown of their assigned districts only (hidden/static label if assigned to only one district)
- Admin profile menu (right) — displays name + role label; dropdown with: Profile Settings, Change Password, Logout

**Sidebar Navigation**
Links visible to District Admins:
- Dashboard (Analytics Home)
- All Incidents
- Settings

Additional links for Super Admin:
- Cross-District Overview
- Manage Districts
- Manage Admins

**Active state, hover states, and collapsed/mobile sidebar behavior** must be consistent with the visual language observed in the Stitch project and `/prototype` folder.

### 4.2 Shell Behaviour
- The shell is not rendered on public-facing screens (Reporting Form, Success Screen, Status Tracker, Login, Forgot Password).
- On mobile, the sidebar collapses to a hamburger menu.
- The district context (which district is currently active) is always visible in the top bar.
- Navigating between sections does not reset the active district context.

---

## 5. Authentication Screens

All auth screens are standalone — no shell, no sidebar. **These screens are not yet designed in Stitch.** Derive the visual language from the Stitch project and `/prototype` folder — same colors, typography, spacing, and form component patterns observed there.

### 5.1 Login Screen
- SAMVAD DMS logo/wordmark
- Email input
- Password input with show/hide toggle
- "Forgot Password?" link below the password field
- Login button (primary CTA)
- No "Sign Up" link — all accounts are admin-provisioned
- Error state: inline message on failed credentials (do not indicate which field is wrong)
- Lockout state: message shown after 5 failed attempts, with a Forgot Password prompt

### 5.2 Forgot Password Screen
- Accessible via the "Forgot Password?" link on Login
- Email input
- Submit button
- Same-screen confirmation message on submit regardless of whether the email exists (do not reveal if an account exists)
- "Back to Login" link

### 5.3 Reset Password Screen
- Accessed via the time-limited link emailed to the admin (valid for 30 minutes)
- New password input + confirm password input
- Password strength indicator
- Submit button
- Expired link state: clear error message with option to request a new reset link
- On success: redirect to Login with a success confirmation banner

---

## 6. Public Screens

These screens have no shell and share no visual language with the admin dashboard.

### 6.1 Public Reporting Form
**Surface:** `[DISTRICT-SLUG]/report` — publicly accessible, no login

Mobile-first. Must be functional on low-bandwidth (3G) connections.

**Fields:**
- **Disaster Type** — select one: Landslide, Flood, Fire, Earthquake, Road Blockage, Others
- **Priority** — Emergency / Standard toggle (Emergency is visually distinct — red/alert styling)
- **Location (GPS)** — browser geolocation capture; user can skip
- **Location (Text)** — free text; required if GPS not captured
- **Media** — photo/video upload or direct camera capture; max 5 files
- **Mobile Number** — 10-digit Indian mobile; receives the tracking SMS

**On Submit:**
- Incident created with status OPEN, assigned to the district of the URL
- Unique Incident ID generated (format: `[DISTRICT-CODE]-YYYYMMDD-XXXX`)
- SMS sent with Incident ID + tracking link
- Transition to Submission Success Screen

### 6.2 Submission Success Screen
Confirms submission. Shows Incident ID and tracking link prominently with a copy button. No further action required.

### 6.3 Status Tracking View
**Surface:** Unique non-guessable URL per incident (sent via SMS)

- Incident ID and Disaster Type
- Visual status stepper: Open → In Progress → Closed
- Last updated timestamp
- Auto-refreshes every 60 seconds or has a manual refresh button
- Shows nothing from the admin side — no comments, no coordinates, no media

---

## 7. Admin Dashboard Screens

### 7.1 Analytics & Dashboard Home

Landing screen after login. Scoped to the active district for District Admins. Scoped to all districts (aggregated) for Super Admins unless a specific district is selected.

**Live Incident Feed**
Real-time list, newest first. Each row: Incident ID, Type (with icon), Priority badge, Location text, Time, Status chip. Clicking a row → Ticket Detail. Filterable by Status, Type, Priority, Date Range. Searchable by ID or location text.

**Analytics Panel**
- Incident Distribution Chart — breakdown by disaster type
- Status Breakdown — Open / In Progress / Closed counts; clicking filters the feed
- KPI Cards: Average Response Time (OPEN→IN PROGRESS), Average Resolution Time (OPEN→CLOSED), Total Incidents (default: last 30 days)

### 7.2 Cross-District Overview *(Super Admin only)*

Aggregated view across all districts. Mirrors the Analytics panel structure but shows data for every district side by side or as a combined total. Includes a district comparison chart. Clicking a district name scopes the dashboard to that district.

### 7.3 All Incidents View

Full paginated list (20 per page). All filter/search options from the feed. Multi-select rows for bulk export. "Export Selected" and "Export All (Filtered)" as Excel. District Admins see only their district's incidents.

### 7.4 Ticket Detail

Sections in order:

**Header:** Incident ID, Type, Priority, Submission time, Current Status + status change control with confirmation step and mandatory note field.

**Location:** GPS map embed (if coordinates exist) + text description.

**Media Gallery:** Submitted photos/videos as thumbnail grid; click for full-size viewer. Admin can upload additional evidence files (labeled "Admin Evidence").

**Status History:** Read-only append-only timeline — each entry shows: previous status → new status, who changed it, when.

**Comment Thread:** Internal admin-only thread. Append-only. Author name + timestamp per comment. Text input + Post button at the bottom.

**Actions:** "Export PDF Report" button.

### 7.5 Manage Districts *(Super Admin only)*

List of all registered districts. Each row: District Name, District Code, Active Admin Count, Total Incidents, Active status toggle. "Add District" button opens a form: District Name, District Code (slug), State, designated public report URL preview.

### 7.6 Manage Admins *(Super Admin only)*

List of all admin accounts across districts. Columns: Name, Email, Role Label, Assigned District(s), Status (Active/Inactive). Actions per row: Edit, Deactivate/Reactivate, Reset Password. "Create Admin" button opens a form: Name, Email, Role Label, Assign District(s). On save, system sends "Set Your Password" email.

### 7.7 User Settings

- Update display name and contact number
- Change password (requires current password)
- Trigger password reset email (for self)
- Super Admins see all admin accounts and can trigger resets on their behalf

---

## 8. Incident Status Model

| Status | Meaning | Set By |
|---|---|---|
| **OPEN** | Submitted, awaiting action | Auto on submission |
| **IN PROGRESS** | Active response underway | District Admin |
| **CLOSED** | Resolved | District Admin |

Transitions are forward-only: OPEN → IN PROGRESS → CLOSED. Direct OPEN → CLOSED is allowed (e.g., duplicate report) and requires a mandatory reason. Closed tickets cannot be reopened through the UI.

Every transition auto-logs: previous status, new status, admin name, UTC timestamp (displayed in IST). This log is immutable.

---

## 9. Reporting & Export

### Excel Export
One row per incident. Covers all key fields: ID, District, Type, Priority, Location, GPS, Mobile, Timestamps, Status, Resolution Note, Media count, Comment count. Filename: `DMS_Export_[DISTRICT]_YYYYMMDD_HHMMSS.xlsx`. Super Admins can export across all districts.

### PDF Incident Report *(per incident)*
Generated from Ticket Detail. Formal printable document:

1. **Header** — SAMVAD DMS wordmark, "INCIDENT REPORT — [DISTRICT NAME]" title, Incident ID, generated timestamp, generated-by admin name, "DISTRICT APPROVAL STAMP" placeholder
2. **Incident Summary** — Type, Priority, District, Location, Submission time, Status at export time
3. **Visual Evidence** — Photos embedded; videos as thumbnail + filename label; "No media submitted" if empty
4. **Operational Narrative** — Full comment history + full status change log, chronological
5. **Footer** — Page numbers, "Confidential — For Internal District Use Only"

---

## 10. Notifications

| Trigger | Channel | Recipient |
|---|---|---|
| New incident submitted | SMS | Citizen — Incident ID + tracking link |
| New incident submitted | Email | All District Admins for that district |
| Status → IN PROGRESS | Email | District Admins + Configured Stakeholders |
| Status → CLOSED | Email | District Admins + Configured Stakeholders |
| Emergency incident submitted | SMS (optional) | Configured admin mobile numbers for that district |

Stakeholder email/SMS lists are configured per district by the Super Admin. Emails include Incident ID, type, priority, location, timestamp, and a direct link to Ticket Detail.

---

## 11. Multi-District Rules

- Every incident belongs to exactly one district, determined by which public reporting URL was used.
- District Admins can be assigned to one or multiple districts by the Super Admin. They can only see, modify, and export incidents within their assigned districts.
- When a District Admin is assigned to multiple districts, the district selector in the shell header lets them switch context between their assigned districts. All dashboard screens — feed, analytics, exports — respond to the active district selection.
- When assigned to only one district, the selector is replaced by a static district label (no dropdown needed).
- Super Admins can switch to any district using the same selector, or view the Cross-District Overview for aggregated data.
- Analytics, exports, and notifications are always district-scoped unless the Super Admin is in Cross-District Overview mode.
- District codes are used in Incident IDs and public report URLs to ensure clear data separation.

---

## 12. Key Constraints

- **No citizen accounts** — citizens interact only via public form + tracking link.
- **Append-only logs** — status history and comments cannot be edited or deleted.
- **Admin accounts are provisioned** — no self-signup for admins. Super Admin creates all accounts.
- **District data isolation** — a District Admin can never access a district they are not assigned to, under any circumstance.
- **Multi-district admins use the selector** — switching districts changes the scope of all dashboard data; no cross-district data is ever shown in a single view except in the Super Admin Cross-District Overview.

---

## 13. Out of Scope (v1)

Do not build:

- Field officer mobile application
- Department-to-department routing or escalation workflows
- Rich text or media embeds in admin comments
- AI-based triage or priority prediction
- Citizen account creation or report history
- In-app push notifications (email + SMS only)
- Multi-language / regional language support

---

*End of Document — SAMVAD DMS PRD v3.1*