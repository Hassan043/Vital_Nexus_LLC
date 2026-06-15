# Shared Database Scripts

Reusable scripts for building and packaging SQL Database Projects.

| Script | Purpose |
|--------|---------|
| [`../scripts/build-dacpacs.sh`](../scripts/build-dacpacs.sh) | Build `VitalNexus.Database.sln`, copy DACPACs to an output folder, and write `dacpac-manifest.json`. Used by CI and deployment workflows. |

Local example:

```bash
bash database/scripts/build-dacpacs.sh database/artifacts/dacpacs
```

Generated output is gitignored under `database/artifacts/`.
