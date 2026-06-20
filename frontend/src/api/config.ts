function readEnv(name: keyof ImportMetaEnv): string {
  return (import.meta.env[name] ?? '').trim()
}

export function getApiBaseUrl(): string {
  const configured = readEnv('VITE_API_BASE_URL')
  if (configured) {
    return configured.replace(/\/$/, '')
  }

  return 'http://localhost:5158'
}

export function isApiConfigured(): boolean {
  return Boolean(getApiBaseUrl())
}
