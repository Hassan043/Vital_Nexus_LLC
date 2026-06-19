import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { RequireAuth } from './RequireAuth'

vi.mock('../auth/useVitalNexusAuth', () => ({
  useVitalNexusAuth: vi.fn(),
}))

vi.mock('../auth/returnUrl', () => ({
  saveAuthReturnUrl: vi.fn(),
}))

import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'
import { saveAuthReturnUrl } from '../auth/returnUrl'

describe('RequireAuth', () => {
  it('redirects guests to sign-in and stores the return URL', () => {
    vi.mocked(useVitalNexusAuth).mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
    } as ReturnType<typeof useVitalNexusAuth>)

    render(
      <MemoryRouter initialEntries={['/reports?tab=1']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/reports" element={<div>Protected</div>} />
          </Route>
          <Route path="/sign-in" element={<div>Sign in page</div>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(saveAuthReturnUrl).toHaveBeenCalledWith('/reports?tab=1')
    expect(screen.getByText('Sign in page')).toBeTruthy()
  })
})
