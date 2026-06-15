# Shared Database Scripts

Reusable scripts for building and packaging SQL Database Projects.

| Script | Purpose |
|--------|---------|
| [`../scripts/build-dacpacs.sh`](../scripts/build-dacpacs.sh) | Build `VitalNexus.Database.sln`, copy DACPACs to an output folder, and write `dacpac-manifest.json`. Used by CI and deployment workflows. |
| [`../scripts/publish-dacpac.sh`](../scripts/publish-dacpac.sh) | Publish a single DACPAC to an Azure SQL database with `sqlpackage`. Used by database deploy workflows. |
| [`../scripts/detect-schema-drift.sh`](../scripts/detect-schema-drift.sh) | Compare a DACPAC to a live Azure SQL database and fail when deploy reports show pending schema changes. |

Local example:

```bash
bash database/scripts/build-dacpacs.sh database/artifacts/dacpacs
```

Generated output is gitignored under `database/artifacts/`.
