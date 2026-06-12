# Deployment Rules

Shared deployment rules and policies for database projects.
# Database Deployment Rules

The SQL Database Projects in `/database` are the source of truth for Azure SQL schema deployment.

- Use DACPAC artifacts as the deployment package.
- Use `sqlpackage` or Azure DevOps/GitHub Actions to deploy schema changes.
- Avoid using EF Core migrations as the production schema deployment mechanism.
- Keep `/data` reserved for static and reference data only.
- Use `PostDeployment.sql` for seed data or runtime initialization scripts.

## Patients database (multi-tenant deployment)

- `VitalNexus.PatientHealth.Database` is the **one** source-controlled schema project for the **Patients database**.
- The same DACPAC is deployed to **many** clinic/customer-specific Patients databases. Each clinic gets its own database instance; this is **not** one database per patient and **not** one SQL project per clinic.
- Set the target database name at publish time (`sqlpackage /TargetDatabaseName:...` or a clinic-specific publish profile).
- PHI and patient clinical records live **only** in tenant-specific Patients databases. The shared Accounts database holds tenant routing metadata only. See `database/VitalNexus.PatientHealth.Database/README.md` for full data-boundary rules.
