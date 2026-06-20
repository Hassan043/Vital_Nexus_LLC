/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_B2C_CLIENT_ID: string
  readonly VITE_B2C_TENANT_ID: string
  readonly VITE_B2C_TENANT_KIND: string
  readonly VITE_B2C_TENANT_DOMAIN_PREFIX: string
  readonly VITE_B2C_USER_FLOW: string
  readonly VITE_B2C_API_SCOPE: string
  readonly VITE_B2C_REDIRECT_URI: string
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
