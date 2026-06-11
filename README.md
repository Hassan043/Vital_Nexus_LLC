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

### 2. Install Dependencies
```bash
# Backend
cd backend
dotnet restore

# Frontend
cd ../frontend
npm ci
```

### 3. Configure Environment Variables (Optional for Email)

Create a `.env` file or configure `appsettings.json` for SMTP:
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "your-email@gmail.com",
    "Pass": "your-app-password",
    "From": "noreply@vitalnexus.com"
  }
}
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

## Getting Started (development)

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
