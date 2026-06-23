const RETURN_URL_KEY = 'vnx.auth.returnTo'

const AUTH_QUERY_PARAMS = [
  'state',
  'code',
  'client_info',
  'session_state',
  'error',
  'error_description',
] as const

function splitReturnPath(path: string): { pathname: string; search: string; hash: string } {
  const hashIndex = path.indexOf('#')
  const hash = hashIndex >= 0 ? path.slice(hashIndex) : ''
  const pathAndSearch = hashIndex >= 0 ? path.slice(0, hashIndex) : path

  const queryIndex = pathAndSearch.indexOf('?')
  if (queryIndex >= 0) {
    return {
      pathname: pathAndSearch.slice(0, queryIndex),
      search: stripAuthQueryParams(pathAndSearch.slice(queryIndex)),
      hash,
    }
  }

  return { pathname: pathAndSearch, search: '', hash }
}

export function stripAuthQueryParams(search: string): string {
  if (!search || search === '?') {
    return ''
  }

  const params = new URLSearchParams(search.startsWith('?') ? search.slice(1) : search)
  for (const param of AUTH_QUERY_PARAMS) {
    params.delete(param)
  }

  const remaining = params.toString()
  return remaining ? `?${remaining}` : ''
}

export function cleanAuthRedirectFromBrowserUrl(): void {
  if (typeof window === 'undefined') {
    return
  }

  const { pathname, search, hash } = window.location
  const cleanedSearch = stripAuthQueryParams(search)

  if (cleanedSearch === search) {
    return
  }

  window.history.replaceState(window.history.state, '', `${pathname}${cleanedSearch}${hash}`)
}

export function formatReturnUrlForDisplay(path: string): string {
  const { pathname, search, hash } = splitReturnPath(path)
  return `${pathname}${search}${hash}`
}

export function sanitizeReturnUrl(path: string): string | null {
  if (!path.startsWith('/') || path.startsWith('//')) {
    return null
  }

  const { pathname, search, hash } = splitReturnPath(path)

  if (pathname.startsWith('/sign-in') || pathname.startsWith('/create-account')) {
    return null
  }

  if (pathname === '/' && !search && !hash) {
    return null
  }

  return `${pathname}${search}${hash}`
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
