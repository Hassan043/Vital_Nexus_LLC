# VitalNexus Azure Functions

Isolated-worker Azure Functions (.NET 8) for asynchronous, retryable, observable
background work:

- AI analysis queue + retry handling
- Vector refresh
- Notification dispatch (email + in-app)
- Retention scans (Year 9 / 9.5 / 10)
- Export package generation
- Stripe webhook follow-up
- Share expiration
- Operational alerts

> **Status:** baseline project added in the next step of Phase 1. Functions must be
> idempotent, must use least-privilege identities, and must never log PHI, raw lab
> values, AI prompts/responses, or clinical notes.
