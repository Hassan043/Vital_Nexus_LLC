const RETURN_URL_KEY = 'vnx.auth.returnTo'

export function sanitizeReturnUrl(path: string): string | null {
  if (!path.startsWith('/') || path.startsWith('//')) {
    return null
  }

  if (path.startsWith('/sign-in') || path.startsWith('/create-account')) {
    return null
  }

  return path
}

export function saveAuthReturnUrl(path: string): void {
  const sanitized = sanitizeReturnUrl(path)
  if (!sanitized) {
    return
  }

  sessionStorage.setItem(RETURN_URL_KEY, sanitized)
}

export function peekAuthReturnUrl(): string | null {
  const value = sessionStorage.getItem(RETURN_URL_KEY)
  return value ? sanitizeReturnUrl(value) : null
}

export function consumeAuthReturnUrl(): string | null {
  const value = peekAuthReturnUrl()
  if (!value) {
    return null
  }

  sessionStorage.removeItem(RETURN_URL_KEY)
  return value
}

export function clearAuthReturnUrl(): void {
  sessionStorage.removeItem(RETURN_URL_KEY)
}
