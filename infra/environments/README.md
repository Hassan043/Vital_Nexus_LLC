# Deployment Environments

VitalNexus uses three isolated Azure deployment environments. Each environment is a separate resource group with its own Bicep parameter file and GitHub Environment for deployment approval gates.

## Environments

| Environment | Resource group | Parameter file | Purpose |
|-------------|----------------|----------------|---------|
| **dev** | `rg-vitalnexus-dev` | `infra/main.dev.bicepparam` | Day-to-day development; lowest-cost SKUs |
| **test** | `rg-vitalnexus-test` | `infra/main.test.bicepparam` | Pre-production integration and QA; prod-like SKUs, non-prod scale |
| **prod** | `rg-vitalnexus-prod` | `infra/main.prod.bicepparam` | Production workloads |

All environments deploy the same `infra/main.bicep` template. The `environmentName` parameter drives resource naming (`-<env>` suffix), tags, and environment-specific defaults (for example prod uses higher replica counts and longer Service Bus message TTL).

## Deploy

### GitHub Actions (recommended)

Run **Deploy Infrastructure** (`.github/workflows/deploy-infra.yml`) manually and select `dev`, `test`, or `prod`.

Each job targets the matching GitHub Environment (`dev`, `test`, or `prod`) for secrets and optional approval gates. Configure these environments in GitHub repository settings before first deploy.

Required secrets (per environment or repository):
- `AZURE_CREDENTIALS`
- `AZURE_SUBSCRIPTION_ID`

Set `SQL_ADMIN_PASSWORD` (and optionally `SQL_ADMIN_LOGIN`) in the workflow or environment secrets before deploying SQL resources.

### Local / CLI

```powershell
$env:SQL_ADMIN_PASSWORD = '<strong-password>'

az group create -n rg-vitalnexus-test -l eastus

az deployment group create `
  -g rg-vitalnexus-test `
  -f infra/main.bicep `
  -p infra/main.test.bicepparam
```

Replace `test` with `dev` or `prod` and the matching parameter file as needed.

## SKU summary

| Setting | dev | test | prod |
|---------|-----|------|------|
| ACR SKU | Basic | Standard | Standard |
| SQL database SKU | Basic | S0 | S0 |
| Log Analytics retention (days) | 30 | 60 | 90 |
| Container min replicas (API) | 0 | 0 | 1 |
| Service Bus message TTL | P7D | P7D | P14D |
| ASP.NET Core environment | Development | Development | Production |

## First-time setup checklist

1. Create GitHub Environments: `dev`, `test`, `prod` (add required reviewers on `test` and `prod` if desired).
2. Create Azure resource groups: `rg-vitalnexus-dev`, `rg-vitalnexus-test`, `rg-vitalnexus-prod`.
3. Configure Azure service principal credentials in GitHub secrets.
4. Run the deploy workflow for each environment.
