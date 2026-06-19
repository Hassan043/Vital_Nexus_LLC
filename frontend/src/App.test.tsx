import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('shows Entra configuration instructions when MSAL env vars are missing', () => {
    vi.stubEnv('VITE_B2C_CLIENT_ID', '')
    vi.stubEnv('VITE_B2C_TENANT_ID', '')

    render(<App />)

    expect(screen.getByRole('heading', { name: 'Configure Entra External ID' })).toBeTruthy()
    expect(screen.getByText(/VITE_B2C_CLIENT_ID/i)).toBeTruthy()
  })
})
