# Deployment Rules

Shared deployment rules and policies for database projects.
# Database Deployment Rules

The SQL Database Projects in `/database` are the source of truth for Azure SQL schema deployment.

- Use DACPAC artifacts as the deployment package.
- Use `sqlpackage` or Azure DevOps/GitHub Actions to deploy schema changes.
- Avoid using EF Core migrations as the production schema deployment mechanism.
- Keep `/data` reserved for static and reference data only.
- Use `PostDeployment.sql` for seed data or runtime initialization scripts.
