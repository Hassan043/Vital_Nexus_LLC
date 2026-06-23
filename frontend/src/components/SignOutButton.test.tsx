import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import { SignOutButton } from './SignOutButton'

const signOut = vi.fn()

vi.mock('../auth/useVitalNexusAuth', () => ({
  useVitalNexusAuth: () => ({
    signOut,
    isLoading: false,
  }),
}))

describe('SignOutButton', () => {
  it('starts Entra sign-out when clicked', () => {
    signOut.mockClear()

    render(<SignOutButton />)

    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }))

    expect(signOut).toHaveBeenCalled()
  })
})
