import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { RequireGuest } from './RequireGuest'

vi.mock('../auth/useVitalNexusAuth', () => ({
  useVitalNexusAuth: vi.fn(),
}))

vi.mock('../auth/returnUrl', () => ({
  consumeAuthReturnUrl: vi.fn(),
}))

import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'
import { consumeAuthReturnUrl } from '../auth/returnUrl'

describe('RequireGuest', () => {
  it('renders guest routes for signed-out users', () => {
    vi.mocked(useVitalNexusAuth).mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
    } as ReturnType<typeof useVitalNexusAuth>)

    render(
      <MemoryRouter initialEntries={['/sign-in']}>
        <Routes>
          <Route element={<RequireGuest />}>
            <Route path="/sign-in" element={<div>Sign in page</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Sign in page')).toBeTruthy()
  })

  it('redirects authenticated users to the stored return URL', () => {
    vi.mocked(useVitalNexusAuth).mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    } as ReturnType<typeof useVitalNexusAuth>)
    vi.mocked(consumeAuthReturnUrl).mockReturnValue('/reports?tab=1')

    render(
      <MemoryRouter initialEntries={['/sign-in']}>
        <Routes>
          <Route element={<RequireGuest />}>
            <Route path="/sign-in" element={<div>Sign in page</div>} />
          </Route>
          <Route path="/reports" element={<div>Reports page</div>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(consumeAuthReturnUrl).toHaveBeenCalled()
    expect(screen.getByText('Reports page')).toBeTruthy()
  })

  it('shows a loading screen while MSAL restores the session', () => {
    vi.mocked(useVitalNexusAuth).mockReturnValue({
      isAuthenticated: false,
      isLoading: true,
    } as ReturnType<typeof useVitalNexusAuth>)

    render(
      <MemoryRouter initialEntries={['/sign-in']}>
        <Routes>
          <Route element={<RequireGuest />}>
            <Route path="/sign-in" element={<div>Sign in page</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Loading sign-in…')).toBeTruthy()
  })
})
