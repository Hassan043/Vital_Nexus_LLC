# Branding image assets

Optional PNG/JPEG files referenced from [`../environments.json`](../environments.json). The configure script uploads them when present.

| File | Graph property | Constraints |
|------|----------------|-------------|
| `banner-logo.png` | `bannerLogo` | PNG or JPEG; recommended transparent, no padding |
| `square-logo.png` | `squareLogo` | PNG or JPEG; square logo for app tiles |
| `favicon.png` | `favicon` | PNG or ICO |

Do not commit customer-facing production artwork here unless the team standardizes on checked-in brand assets. For a first pass, run the configure script without assets (text and colors only), then add logos in the portal or drop files here and re-run.

Convert `frontend/public/favicon.svg` to PNG locally if you need a starter favicon.
