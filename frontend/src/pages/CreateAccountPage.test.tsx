import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { CreateAccountPage } from './CreateAccountPage'

vi.mock('../auth/useVitalNexusAuth', () => ({
  useVitalNexusAuth: () => ({
    signUp: vi.fn(),
    isLoading: false,
  }),
}))

describe('CreateAccountPage', () => {
  it('walks through the registration steps before redirecting', () => {
    render(
      <MemoryRouter>
        <CreateAccountPage />
      </MemoryRouter>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Get started' }))
    expect(screen.getByRole('heading', { name: 'Your work email' })).toBeTruthy()

    fireEvent.change(screen.getByLabelText('Email address'), {
      target: { value: 'clinician@example.com' },
    })
    fireEvent.click(screen.getByRole('checkbox'))
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))

    expect(screen.getByRole('heading', { name: 'Finish on Microsoft' })).toBeTruthy()
    expect(screen.getByText(/clinician@example.com/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Continue to secure registration' })).toBeTruthy()
  })
})
