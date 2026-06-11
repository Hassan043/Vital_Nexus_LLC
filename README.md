# VitalNexus

Health technology SaaS platform for functional medicine clinics, independent
practitioners, and wellness professionals. Providers track patient lab results
over time and receive AI-supported, **longitudinal** clinical insights based on
each patient's complete history.

> **Confidential** — for internal use only.

## Core Principles

- Every patient has a permanent, append-only health record that is never overwritten.
- Every lab result is a timestamped entry; every AI analysis is saved and reused as future context.
- Strict PHI isolation — patient health data lives in a separate database from account/business data.
- **No patient health information is ever sent to the AI engine** — only anonymized lab values and reference ranges.
- No record is ever automatically deleted.

## Technology Stack

| Layer            | Technology                                                        |
| ---------------- | ----------------------------------------------------------------- |
| Backend API      | .NET 8 ASP.NET Core Web API                                        |
| Background jobs  | Azure Functions (isolated worker, .NET 8)                         |
| Data access      | Entity Framework Core — separate `DbContext` per database         |
| Databases        | Azure SQL — Account/Business DB + Patient Health DB (PHI)         |
| Vector store     | Vector Intelligence Store (Azure AI Search / pgvector — TBD)      |
| Frontend         | React + Vite (added in a later phase)                             |
| AI engine        | Claude API                                                        |
| Billing          | Stripe                                                            |
| Observability    | Azure Application Insights / Azure Monitor                        |
| Infrastructure   | Bicep (IaC), CI/CD pipelines                                      |

## Repository Layout

```
VitalNexus/
├── docs/                      Source requirements & technical design documents
├── backend/                   .NET 8 solution (VitalNexus.sln)
│   ├── src/
│   │   ├── Core/
│   │   │   ├── VitalNexus.Domain/         Entities, enums, value objects (no dependencies)
│   │   │   └── VitalNexus.Application/    Use cases, service interfaces, DTOs
│   │   ├── Infrastructure/
│   │   │   └── VitalNexus.Infrastructure/ EF Core DbContexts, data access, external services
│   │   ├── Api/
│   │   │   └── VitalNexus.Api/            ASP.NET Core Web API (the only synchronous data-store access point)
│   │   └── Functions/                     Azure Functions — async AI, retention, export, notifications
│   └── tests/
│       ├── VitalNexus.UnitTests/
│       └── VitalNexus.IntegrationTests/
├── frontend/                  React + Vite + TypeScript SPA (Provider / Clinic Owner / Master Admin portals)
├── infra/                     Bicep templates & deployment (added during Phase 1)
└── scripts/                   Developer & ops helper scripts
```

## Architecture Rules

- The frontend never touches a database directly. **All** auth, RBAC, patient-scope
  resolution, PHI isolation, AI prompt construction, audit logging, and billing
  validation happen in the backend.
- The backend API is the only synchronous service permitted to access the three data stores.
- Cross-store joins are performed in application memory **after** authorization — never via direct DB links.
- Application Insights must never receive PHI, raw lab values, AI prompts/responses, or clinical notes.
- No monetary value is hard-coded — prices, fees, discounts, and Stripe price IDs are database-managed.

## Implementation Roadmap

| Phase | Focus                                       |
| ----- | ------------------------------------------- |
| **1** | **Foundation** — infra, identity, DB separation, auth/MFA, RBAC, audit, Functions + App Insights baseline |
| 2     | Clinic operations — onboarding, billing, licenses |
| 3     | Patient & lab workflows                      |
| 4     | AI & vector intelligence                      |
| 5     | Archive, retention, export, deletion          |
| 6     | Hardening & launch                            |

### 5. Access Application
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5000
- **API Documentation**: http://localhost:5000/swagger

## 🔐 Authentication Features

### Register
- Navigate to `/register`
- Email + password + confirm password
- Auto-login after registration

### Login
- Navigate to `/login`
- Email + password
- Show/hide password toggle

### Forgot Password
- Click "Forgot password?" on login page
- Enter email
- Receive reset link (email or console if SMTP not configured)
- Link expires in 60 minutes

### Reset Password
- Click link from email
- Enter new password + confirm
- Redirects to login on success

## 🗄️ Database

Uses **SQLite** for local development. The database file (`nutrient.db`) is created automatically on first run.

### Tables
- Users
- LabReports (with `reportPublicId` for privacy)
- LabMarkers
- PetProfiles
- PasswordResetTokens
- BodyTendencyProfiles
- ExercisePlans

## Database Schema Management
The `/database` directory contains SQL Database Projects that are the authoritative source of truth for Azure SQL schema.

- `/database` contains the SQL Database Projects for account business, patient health, and function operations.
- SQL Database Projects are the source of truth for schema and object deployment.
- Deployment uses `DACPAC` artifacts and `sqlpackage` for Azure SQL publishing.
- EF Core in `backend/` is used for runtime data access only.
- EF Core migrations are not the production schema deployment mechanism.
- `/data` is reserved for static/reference data files such as `markers.json`.

## 📊 Supported Lab Markers

### CBC
- White Blood Cell Count (WBC)
- Red Blood Cell Count (RBC)
- Hemoglobin
- Hematocrit
- Platelets

### Metabolic Panel
- Glucose (Fasting)
- Sodium, Potassium, Chloride
- CO2 (Bicarbonate)
- BUN, Creatinine, Calcium
- Fasting Insulin

### Lipid Panel
- Total Cholesterol
- LDL Cholesterol
- HDL Cholesterol
- Triglycerides

### Thyroid
- TSH, Free T4, Free T3

### Vitamins & Minerals
- Vitamin D (25-OH)
- Vitamin B12
- Magnesium

### Iron Panel
- Ferritin
- Serum Iron
- TIBC
- Transferrin Saturation

### Liver
- ALT, AST, ALP
- Bilirubin (Total)
- Total Protein, Albumin, Globulin

### Inflammation
- CRP, hs-CRP

## 🔒 Security & Privacy

### Privacy-First Design
- No storage of full name, DOB, address, phone, or MRN
- Email only used for authentication
- Reports identified by `reportPublicId` (e.g., VN-20250215-A3X9K2)
- No email in exports or UI display
- User-specific data isolation via UUID

### Authentication Security
- BCrypt password hashing (cost factor 10)
- JWT with HS256 signing (24-hour expiration)
- Password reset tokens use SHA-256 hashing
- Single-use reset tokens with 60-minute expiration
- Rate limiting considerations for forgot password

### Logging
- Authorization headers redacted in logs
- No request body logging for marker values
- No PHI in application logs

## 📤 Export Formats

- **Excel (.xlsx)**: Multi-tab report with tracking templates
- **PDF/HTML**: Complete wellness report with educational content

Exports use `reportPublicId` for filenames, never email or personal info.

## 🧪 Testing Password Reset Locally

If SMTP is not configured:

1. Submit forgot password request
2. Check backend console logs for reset link
3. Copy link and paste in browser
4. Set new password

Example log output:
```
SMTP not configured. Reset link: http://localhost:5173/reset-password?token=ABC123...
```

Prerequisites: .NET SDK (8/9/10), Node.js 20+, and the Azure Functions Core Tools
(for running Functions locally).

```bash
# Backend
cd backend
dotnet restore
dotnet build
dotnet test

# Frontend
cd ../frontend
npm install
npm run dev
```

> Source of truth for requirements: `docs/VitalNexus Platform Requirements Document.pdf`
> and the accompanying Technical Design Document.
