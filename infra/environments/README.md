# Deployment Environments

VitalNexus uses three isolated Azure deployment environments. Each environment is a separate resource group with its own Bicep parameter file and GitHub Environment for deployment approval gates.

## Environments

| Environment | Resource group | Parameter file | Purpose |
|-------------|----------------|----------------|---------|
| **dev** | `rg-vitalnexus-dev` | `infra/main.dev.bicepparam` | Day-to-day development; lowest-cost SKUs |
| **test** | `rg-vitalnexus-test` | `infra/main.test.bicepparam` | Pre-production integration and QA; prod-like SKUs, non-prod scale |
| **prod** | `rg-vitalnexus-prod` | `infra/main.prod.bicepparam` | Production workloads |

All environments deploy the same `infra/main.bicep` template. The `environmentName` parameter drives resource naming (`-<env>` suffix), tags, and environment-specific defaults (for example prod uses higher replica counts and longer Service Bus message TTL).

Each environment also receives **dedicated SQL servers, databases, Key Vault secrets, and Service Bus namespace** — nothing is shared across dev, test, or prod. See [`secrets-and-databases.md`](secrets-and-databases.md) for the full isolation model and required GitHub secrets.

## Deploy

### GitHub Actions (recommended)

Run **Deploy Infrastructure** (`.github/workflows/deploy-infra.yml`) manually and select `dev`, `test`, or `prod`.

Each job targets the matching GitHub Environment (`dev`, `test`, or `prod`) for secrets and optional approval gates. Configure these environments in GitHub repository settings before first deploy.

Required secrets (configure **separately** in each GitHub Environment — do not share prod credentials with dev or test):
- `AZURE_CREDENTIALS`
- `AZURE_SUBSCRIPTION_ID`
- `SQL_ADMIN_PASSWORD` (unique per environment)
- `SQL_ADMIN_LOGIN` (optional; defaults to `vnxadmin`)

The deploy workflow injects SQL credentials at deploy time. See [`secrets-and-databases.md`](secrets-and-databases.md) for the full secrets and database isolation model.

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

Application components (frontend, API, Functions) are deployed separately — see [`docs/DEPLOYMENT_PIPELINES.md`](../docs/DEPLOYMENT_PIPELINES.md).
