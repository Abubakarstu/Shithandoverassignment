# Gulf Air — Shift Handover System

An ASP.NET Core **MVC** application (.NET 9) that lets airport shift supervisors claim a
duty slot, log accidents / incidents / manpower during that duty, close the shift when the
duty ends, and automatically generate a **PDF handover report** that is e-mailed to every
other supervisor.

---

## 1. Quick start

```bash
# 1. Restore and build
dotnet restore
dotnet build

# 2. Create the database (LocalDB) and apply migrations
dotnet tool restore
dotnet dotnet-ef database update

# 3. Run
dotnet run
```

Then open the URL printed in the console (`https://localhost:7077` by default).

> ### Deploying to hosting?
> See **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** for the full MonsterASP / Plesk walkthrough,
> including HTTP-only configuration, the self-provisioning database, and go-live hardening.

On first start the application applies any pending migrations **and** seeds the database:

* 4 supervisor accounts
* one fully worked **closed** shift (2 accidents, 2 incidents, 4 manpower rows, generated PDF)
* 21 unclaimed duty slots covering today and the next 6 days

### Demo accounts

Password for every seeded account: `Pass@123`

| Name | Email | Role |
|---|---|---|
| Sara Al Mansoori | `sara.almansoori@gulfair.test` | **Admin** (can generate the shift roster, re-send reports) |
| Mohammed Al Zaabi | `mohammed.alzaabi@gulfair.test` | Supervisor |
| Fatima Al Blooshi | `fatima.alblooshi@gulfair.test` | Supervisor |
| Omar Al Suwaidi | `omar.alsuwaidi@gulfair.test` | Supervisor |

> **To try the locking rule you need two browsers** (or one normal + one private window):
> sign in as Mohammed, claim a shift, then sign in as Fatima in the other window and open the
> same shift.

---

## 2. Feature walkthrough

| Requirement | Where it lives | How to demonstrate it |
|---|---|---|
| **User authentication** | `Controllers/AccountController.cs`, `Program.cs` (cookie auth) | Sign in / sign out. Unauthenticated requests to any page are redirected to `/Account/Login`. Only an **Admin** can open `/Shifts/Generate` (non-admins get a 403). |
| **Shift claiming** | `ShiftService.ClaimShiftAsync`, `Views/Shifts/Details.cshtml` | Press **Claim** on an open shift. The status turns to *Claimed* and the shift becomes read-only for everyone else. Open the same shift as a second supervisor — the Claim button is gone and a "Locked" banner is shown. |
| **Shift logging** | `AccidentsController`, `IncidentsController`, `ManpowerController`, `ShiftLogService` | On a shift you claimed, use the *Quick add* buttons. Accidents capture time, type, severity, location, injuries, description, immediate action and a reportable-to-regulator flag. |
| **Shift closing** | `ShiftService.CloseShiftAsync` | **Close shift & send handover**. Status becomes *Closed*, every edit control disappears, and a banner explains the shift is read-only. |
| **PDF report** | `ReportService.cs` (QuestPDF) | `/Reports` → **Preview** or **Download**. One A4 page with a KPI strip, shift header, accidents table, incidents table, manpower table with totals, handover notes and signature blocks. |
| **Automatic e-mail** | `ReportDispatchService.cs`, `SmtpEmailSender.cs` | Closing the shift sends the PDF to **every other active supervisor**. Each attempt is recorded in `/Reports/EmailLog`. |

### Manual verification script (5 minutes)

1. Sign in as **Mohammed**, open **Shifts**, claim *today's Morning shift*.
2. Add one accident, one incident and two manpower rows.
3. Sign in as **Fatima** in a private window, open the same shift → **Locked**, no edit buttons.
4. Back as **Mohammed**: **Close shift & send handover** → success message naming the recipients.
5. Open `/Reports` → **Preview** the PDF. Open `/Reports/EmailLog` → the delivery record.
6. Try to edit anything on the closed shift → every path is rejected, including a hand-crafted POST.

---

## 3. Email configuration

Out of the box **no SMTP host is configured**, so closing a shift writes the message and its
PDF attachment to `App_Data/EmailOutbox/` and logs the delivery as `QueuedToOutbox`. This keeps
the demo fully functional offline. To send real e-mail, fill in `appsettings.json`:

```json
"EmailSettings": {
  "Enabled": true,
  "Host": "smtp.office365.com",
  "Port": 587,
  "EnableSsl": true,
  "UserName": "shift-handover@gulfair.com",
  "Password": "<your password or app password>",
  "FromAddress": "shift-handover@gulfair.com",
  "FromDisplayName": "Shift Handover System",
  "AdditionalRecipients": "station.manager@gulfair.com, hse@gulfair.com",
  "NotifyAllActiveSupervisors": true,
  "TimeoutSeconds": 15
}
```

| Setting | Effect |
|---|---|
| `NotifyAllActiveSupervisors` | `true` → every *other* active supervisor is a recipient. `false` → only `AdditionalRecipients`. |
| `AdditionalRecipients` | Extra static recipients (station manager, HSE inbox…). Comma separated. |
| `Enabled` | `false` → skip SMTP entirely and always use the local outbox. |

> **Security note for a production deployment:** keep the SMTP password in user-secrets,
> Azure Key Vault or environment variables (`EmailSettings__Password`), never in
> `appsettings.json`.

---

## 4. Solution layout

```
ShiftHandover/
├── Controllers/            MVC controllers (thin: they call services, never the DbContext for rules)
│   ├── AccountController.cs        login / logout
│   ├── HomeController.cs           dashboard
│   ├── ShiftsController.cs         list, details, claim, release, close, roster generation
│   ├── AccidentsController.cs      accident CRUD for a shift
│   ├── IncidentsController.cs      incident CRUD for a shift
│   ├── ManpowerController.cs       manpower CRUD for a shift
│   └── ReportsController.cs        PDF preview / download, e-mail log, re-send
├── Data/
│   ├── ShiftHandoverContext.cs     EF Core DbContext, keys, indexes, delete behaviour
│   ├── SeedData.cs                 demo accounts + worked example shift
│   ├── DesignTimeDbContextFactory.cs   lets `dotnet ef` work without booting the host
│   └── Migrations/                 InitialCreate
├── Models/
│   ├── Entities/                   Supervisor, Shift, Accident, Incident,
│   │                               ManpowerRecord, ShiftReport, EmailLog, AuditLog
│   ├── Enums/                      ShiftStatus, SeverityLevel, AccidentType, ...
│   ├── Reports/ShiftReportData.cs  aggregate handed to the PDF template
│   └── ViewModels/                 form and list models with validation attributes
├── Services/                       all business rules live here
│   ├── IShiftService / ShiftService            claim, close, the editability guard
│   ├── IShiftLogService / ShiftLogService      accident / incident / manpower CRUD
│   ├── IReportService / ReportService          QuestPDF template
│   ├── IReportDispatchService / …Service       close → PDF → e-mail orchestration
│   ├── IEmailSender / SmtpEmailSender          SMTP with local outbox fallback
│   ├── EmailOptions.cs                        bound from appsettings
│   ├── PasswordHasher.cs                      PBKDF2-SHA256, 100k iterations
│   ├── PdfStyles.cs                           PDF colours and cell styles
│   └── OperationResult.cs                     result type for service calls
├── Views/                          Razor views per controller
├── Helpers/                        claim extensions, formatting, shift type definitions
├── docs/
│   ├── ERD.md                     entity relationship diagram + explanation
│   └── schema.sql                 generated DDL script
└── App_Data/                       runtime output (git-ignored)
    ├── Reports/                   generated PDFs
    └── EmailOutbox/               e-mails that could not be delivered over SMTP
```

---

## 5. Architecture notes

**Layering.** `Controller → Service → DbContext`. Controllers never decide whether a write is
allowed; they call `IShiftService.GetEditableShiftAsync(...)` (via `ShiftLogService`) and turn a
failure into a message. The rules are therefore enforced even if someone bypasses the UI.

**Authentication.** Cookie authentication with an 8-hour sliding expiry. Passwords are stored as
PBKDF2-SHA256 (100,000 iterations, 16-byte random per-user salt) and compared with
`CryptographicOperations.FixedTimeEquals`. Login failures return one generic message so the form
cannot be used to discover which e-mail addresses exist. `[Authorize]` is applied at controller
level; `[AllowAnonymous]` only on login and error pages.

**Claiming is race-safe.** The claim is a single conditional `UPDATE`
(`WHERE Status = Open AND ClaimedById IS NULL`) via `ExecuteUpdateAsync`, so two supervisors
clicking *Claim* at the same instant cannot both win — the loser sees an explanatory message.
`RowVersion` on `Shift` additionally protects against lost updates while a shift is being edited.

**Closing is irreversible and audited.** `CloseShiftAsync` writes `Status = Closed` first and only
then generates the PDF and sends the e-mail. If report generation fails the shift stays closed
(the duty *did* end) and the error is surfaced to the supervisor instead of being swallowed.

**PDF generation.** `QuestPDF` builds the document from a `ShiftReportData` aggregate loaded in a
single split query, so the template never touches the database.

**Split queries.** Any page that loads three collections at once (shift details, the report) uses
`QuerySplittingBehavior.SplitQuery` to avoid a cartesian explosion.

---

## 6. Database management

```bash
dotnet tool restore                      # restore the pinned dotnet-ef local tool

dotnet dotnet-ef database update         # apply migrations
dotnet dotnet-ef migrations add <Name>   # add a migration
dotnet dotnet-ef migrations script --idempotent --output docs/schema.sql
dotnet dotnet-ef database drop           # reset (re-seeds on next start)
```

Set `Database:ApplyMigrationsOnStartup` to `false` to manage the schema purely from the CLI.

To use a different server, change `ConnectionStrings:DefaultConnection` in `appsettings.json`, or
set the `SHIFT_HANDOVER_CONNECTION` environment variable when running `dotnet ef`.

---

## 7. Known limitations / next steps

* E-mail is sent synchronously during the close request. For production, move
  `GenerateAndDistributeAsync` to a background channel (a hosted service + `Channel<T>` or
  Hangfire) and let the supervisor close the shift instantly.
* Reports are stored on the local file system under `App_Data`. Move them to blob storage
  (Azure Blob / S3) when running more than one instance.
* `Supervisor` management (create / deactivate users) is seeded only; an admin CRUD screen would be
  the natural next slice.
* Shift slots are generated for three fixed shift types. A `ShiftTemplate` table would let an
  admin define the roster pattern (e.g. 06:00–14:00 / 14:00–22:00 only at certain terminals).