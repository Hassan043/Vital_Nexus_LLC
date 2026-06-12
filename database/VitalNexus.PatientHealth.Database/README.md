# Patients SQL Database Project

`VitalNexus.PatientHealth.Database` is the **single source-controlled schema project** for the **Patients database** — the authoritative store for patient-specific clinical data (PHI).

## One project, many deployed databases

| Concept | Detail |
|--------|--------|
| SQL Database Project | **One** project in source control (`VitalNexus.PatientHealth.Database`) |
| Deployed databases | **Many** — one dedicated Patients database per clinic/customer |
| Not per patient | A clinic's Patients database holds **all** patients for that tenant |
| Not per clinic project | There is **not** one SQL project per clinic |

Each clinic/customer receives its own Azure SQL database built from the **same DACPAC**. The target database name is chosen at deploy time (for example `Patients`, `Patients-AcmeClinic`, or an environment-specific name). Infrastructure provisioning creates the empty database; schema is applied with `sqlpackage` from this project.

Example deploy to a clinic-specific database:

```powershell
sqlpackage /Action:Publish `
  /SourceFile:.\bin\Release\VitalNexus.PatientHealth.Database.dacpac `
  /TargetServerName:sql-vitalnexus-phi-prod.database.windows.net `
  /TargetDatabaseName:Patients-AcmeClinic `
  /TargetUser:... /TargetPassword:...
```

See `PublishProfiles/ClinicPatients.publish.xml` for a publish-profile template. Set `TargetDatabaseName` per clinic when publishing.

## Data boundaries

### Patients database (this project) — PHI source of truth

- **Single-tenant per clinic/customer.** All patient clinical records for a tenant live in that tenant's Patients database only.
- Stores PHI and patient clinical records: demographics, lab results, clinical notes, analyses tied to identifiable patients, and related audit data for that tenant.
- Never shared across clinics. No cross-database joins at the SQL layer — the application resolves tenant scope and connects to the correct Patients database after authorization.

### Accounts database — routing metadata only

- Shared across the platform (`VitalNexus.AccountBusiness.Database`).
- Stores accounts, billing, users, and **tenant-to-Patients-database routing metadata** (connection targets, tenant identifiers).
- **Does not store PHI** or patient clinical records.

### LabMarkersData database — shared reference data

- Shared across the platform (`VitalNexus.LabMarkersData.Database`).
- Stores lab marker definitions, units, categories, and reference ranges.
- **Non-patient, non-PHI** master data used by all tenants.

### AI / Vector Intelligence Store — anonymized retrieval only

- Shared store for AI retrieval embeddings and search indexes.
- Holds **only anonymized** lab values and derived intelligence suitable for cross-tenant AI features.
- **Not the source of truth** for patient records. Authoritative clinical data always remains in the tenant-specific Patients database.

## Schema deployment

- This SQL Database Project is the **source of truth** for Patients schema. EF Core is runtime access only; EF migrations are not used for production schema deployment.
- Build produces a DACPAC artifact. Deploy with `sqlpackage` or CI/CD pipelines.
- Seed and post-deploy scripts belong in `PostDeployment.sql` and `ReferenceData/` when needed.

## Folder layout

- `Tables/` — patient clinical entities
- `Views/` — reporting and access views
- `StoredProcedures/` — data access procedures
- `Security/` — roles and permissions scoped to the tenant database
- `ReferenceData/` — tenant-local reference data if required
- `PublishProfiles/` — per-clinic publish profile templates
