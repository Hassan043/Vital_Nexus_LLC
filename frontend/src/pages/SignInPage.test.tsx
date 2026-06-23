import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { SignInPage } from './SignInPage'

const signIn = vi.fn()

vi.mock('../auth/useVitalNexusAuth', () => ({
  useVitalNexusAuth: () => ({
    signIn,
    isLoading: false,
  }),
}))

describe('SignInPage', () => {
  it('shows the return path when redirected from a protected route', () => {
    render(
      <MemoryRouter
        initialEntries={[
          {
            pathname: '/sign-in',
            state: { from: { pathname: '/reports', search: '?tab=1' } },
          },
        ]}
      >
        <SignInPage />
      </MemoryRouter>,
    )

    expect(screen.getByText(/Sign in to continue to/i)).toBeTruthy()
    expect(screen.getByText('/reports?tab=1')).toBeTruthy()
  })

  it('starts Entra sign-in with a login hint for valid email addresses', async () => {
    signIn.mockClear()

    render(
      <MemoryRouter>
        <SignInPage />
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('Email address (optional)'), {
      target: { value: 'clinician@example.com' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(signIn).toHaveBeenCalledWith('clinician@example.com')
  })

  it('ignores invalid email values when starting sign-in', async () => {
    signIn.mockClear()

    render(
      <MemoryRouter>
        <SignInPage />
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('Email address (optional)'), {
      target: { value: 'not-an-email' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(signIn).toHaveBeenCalledWith(undefined)
  })
})
