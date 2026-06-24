export class ApiError extends Error {
  readonly status: number
  readonly body: string

  constructor(status: number, body: string) {
    super(`API request failed (${status})`)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }

  get displayMessage(): string {
    try {
      const parsed = JSON.parse(this.body) as { error?: string; title?: string }
      return parsed.error ?? parsed.title ?? this.body
    } catch {
      return this.body || this.message
    }
  }
}

export type ApiClient = {
  get: <T>(path: string) => Promise<T>
  post: <T>(path: string, body: unknown) => Promise<T>
  put: <T>(path: string, body: unknown) => Promise<T>
}

type CreateApiClientOptions = {
  baseUrl: string
  getAccessToken: () => Promise<string>
}

export function createApiClient({ baseUrl, getAccessToken }: CreateApiClientOptions): ApiClient {
  const normalizedBaseUrl = baseUrl.replace(/\/$/, '')

  async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const token = await getAccessToken()
    const response = await fetch(`${normalizedBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${token}`,
        ...init.headers,
      },
    })

    const body = await response.text()
    if (!response.ok) {
      throw new ApiError(response.status, body)
    }

    if (!body) {
      return undefined as T
    }

    return JSON.parse(body) as T
  }

  return {
    get: <T>(path: string) => request<T>(path, { method: 'GET' }),
    post: <T>(path: string, body: unknown) =>
      request<T>(path, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      }),
    put: <T>(path: string, body: unknown) =>
      request<T>(path, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      }),
  }
}
