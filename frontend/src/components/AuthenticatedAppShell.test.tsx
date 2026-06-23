import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthenticatedAppShell } from './AuthenticatedAppShell'
import { createAccount } from '../test/msalTestDoubles'

vi.mock('../auth/useVitalNexusAuth', () => ({
  useVitalNexusAuth: vi.fn(),
}))

vi.mock('./SignOutButton', () => ({
  SignOutButton: () => <button type="button">Sign out</button>,
}))

import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

describe('AuthenticatedAppShell', () => {
  it('shows the signed-in user in the session header', () => {
    vi.mocked(useVitalNexusAuth).mockReturnValue({
      account: createAccount({
        name: 'Dr. Example',
        username: 'doctor@example.com',
      }),
    } as ReturnType<typeof useVitalNexusAuth>)

    render(
      <MemoryRouter>
        <AuthenticatedAppShell />
      </MemoryRouter>,
    )

    expect(screen.getByText('Signed in')).toBeTruthy()
    expect(screen.getByText('Dr. Example')).toBeTruthy()
    expect(screen.getByText('doctor@example.com')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeTruthy()
  })
})
