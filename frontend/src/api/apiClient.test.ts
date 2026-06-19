import { describe, expect, it, vi } from 'vitest'
import { createApiClient } from './apiClient'

describe('createApiClient', () => {
  it('sends bearer tokens on GET requests', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      text: async () => JSON.stringify({ status: 'ok' }),
    })
    vi.stubGlobal('fetch', fetchMock)

    const api = createApiClient({
      baseUrl: 'http://localhost:5158',
      getAccessToken: async () => 'test-token',
    })

    await api.get('/api/me')

    expect(fetchMock).toHaveBeenCalledWith('http://localhost:5158/api/me', {
      method: 'GET',
      headers: {
        Accept: 'application/json',
        Authorization: 'Bearer test-token',
      },
    })
  })
})
