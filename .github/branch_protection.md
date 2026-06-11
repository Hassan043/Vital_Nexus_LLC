# Branch protection for `dev`

This file documents the recommended branch-protection settings for the `dev` branch and provides a sample GitHub API payload to apply them.

Recommended rules for `dev`:
- Require pull requests for all changes (no direct pushes by contributors).
- Require 2 approving reviews before merge.
- Require code owner reviews (enforced by the `CODEOWNERS` file).
- Require status checks to pass (CI/build and tests).
- Require branches to be up-to-date with `dev` before merging (strict status checks).
- Require all conversations to be resolved.
- Restrict who can push to the branch (admins only or a small maintainer team).
- (Optional) Require signed commits.

Workflow note:
- Develop issue branches from `dev` and open PRs against `dev`.
- After `dev` is reviewed and merged, open the release PR from `dev` into `main`.
- Protect `main` so only reviewed, tested changes from `dev` can be merged.

Sample GitHub REST API payload to apply branch protection (replace `OWNER`, `REPO`, and `contexts`):

```
PUT /repos/OWNER/REPO/branches/dev/protection

{
  "required_status_checks": {
    "strict": true,
    "contexts": ["ci/build", "ci/tests"]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": true,
    "required_approving_review_count": 2
  },
  "restrictions": {
    "users": [],
    "teams": []
  }
}
```

Apply with `curl` (set `GITHUB_TOKEN` with repo admin scope):

```bash
curl -X PUT \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  https://api.github.com/repos/OWNER/REPO/branches/dev/protection \
  -d '@payload.json'
```

Or configure these rules in the GitHub UI under `Settings -> Branches -> Branch protection rules`.

## Branch protection for `main`

For `main`, use stricter protection than `dev` because this is the release/stable branch.

Recommended rules for `main`:
- Require pull requests for all changes (no direct pushes by contributors).
- Require 2 approving reviews before merge.
- Require code owner reviews via the `CODEOWNERS` file.
- Require status checks to pass (CI/build and tests).
- Require branches to be up-to-date with `main` before merging (strict status checks).
- Require all conversations to be resolved.
- Restrict who can push to `main` (admins/maintainers only).
- Optionally require signed commits and enforce linear history.

Sample GitHub REST API payload to apply branch protection for `main`:

```
PUT /repos/OWNER/REPO/branches/main/protection

{
  "required_status_checks": {
    "strict": true,
    "contexts": ["ci/build", "ci/tests"]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": true,
    "required_approving_review_count": 2
  },
  "restrictions": {
    "users": [],
    "teams": []
  }
}
```

Apply with `curl` (set `GITHUB_TOKEN` with repo admin scope):

```bash
curl -X PUT \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  https://api.github.com/repos/OWNER/REPO/branches/main/protection \
  -d @.github/protection-main.json
```
