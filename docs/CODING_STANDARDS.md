# VitalNexus Coding Standards

These standards apply to all code in the VitalNexus repository. They exist to keep
the codebase consistent, secure, and HIPAA-aware. **Issue F1.T1.6.**

## 1. Golden Rules (non-negotiable)

1. **Never log PHI.** No patient names, DOB, lab values, clinical notes, AI prompts,
   or AI responses may reach logs, telemetry, Application Insights, or exception
   messages. Use operation IDs, clinic IDs, and the **anonymous patient identifier**.
2. **Never hard-code money.** Subscription prices, fees, discounts, and Stripe price
   IDs come from the database at runtime — never from source code, config, env vars,
   Bicep, CI/CD, or migration defaults. Use `decimal`, never floating point, for money.
3. **The frontend never touches a database.** All auth, RBAC, patient-scope checks,
   PHI isolation, AI prompt building, audit logging, and billing validation live in
   the backend.
4. **Respect database separation.** Account/Business DB and Patient Health DB are
   isolated. No cross-database joins in SQL — combine in application memory *after*
   authorization.
5. **Audit everything sensitive.** PHI access, shares, result entry, analyses, notes,
   overrides, exports, archive/restore, deletions, legal holds, password resets, MFA
   events, and pricing changes are all audit-logged. Audit logs are append-only.

## 2. C# / .NET

- Target framework: **net8.0**. Nullable reference types and implicit usings enabled.
- Use **file-scoped namespaces** and `async`/`await` for all I/O. Suffix async methods
  with `Async`.
- Prefer constructor dependency injection; no service locator.
- One top-level type per file; file name matches the type.
- Public APIs validate input at the boundary; never trust client-supplied amounts or IDs.
- Use `decimal(10,2)` columns for monetary values.
- Background work (AI analysis, exports, retention scans, notifications) runs in
  **Azure Functions** and must be **idempotent and safe to retry**.

## 3. Naming

| Element                         | Convention            |
| ------------------------------- | --------------------- |
| Classes, methods, properties    | `PascalCase`          |
| Local variables, parameters     | `camelCase`           |
| Private fields                  | `_camelCase`          |
| Constants                       | `PascalCase`          |
| Interfaces                      | `IPascalCase`         |
| Async methods                   | `...Async`            |

## 4. Git Workflow

- `main` is protected — no direct pushes. All changes go through a Pull Request.
- Branch naming: `feature/F<n>.T<m>.<k>-short-slug`, `fix/short-slug`, `chore/short-slug`.
- Commit messages: imperative mood, reference the issue, e.g.
  `F1.T1.4 Add pull request template`.
- Keep PRs small and focused on a single issue. Fill out the PR checklist.
- Rebase or merge `main` before requesting review; CI must be green.

## 5. Testing

- Unit tests for domain/application logic; integration tests for API + data access.
- Tests must **never** contain real PHI — use clearly fake sample data.
- A bug fix should add a regression test where practical.

## 6. Secrets & Configuration

- No secrets in source control. Use `local.settings.json` (gitignored) locally and
  Azure Key Vault in cloud environments.
- `appsettings.json` holds non-secret defaults only; never real connection strings or keys.
