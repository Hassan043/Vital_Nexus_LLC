export class ApiError extends Error {
  readonly status: number
  readonly body: string

  constructor(status: number, body: string) {
    super(`API request failed (${status})`)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

export type ApiClient = {
  get: <T>(path: string) => Promise<T>
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
  }
}
