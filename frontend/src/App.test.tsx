import { describe, expect, it } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('renders the get started heading', () => {
    render(<App />)

    expect(screen.getByRole('heading', { name: 'Get started' })).toBeTruthy()
  })

  it('increments the counter when the button is clicked', () => {
    render(<App />)

    const button = screen.getByRole('button', { name: /count is 0/i })
    fireEvent.click(button)

    expect(screen.getByRole('button', { name: /count is 1/i })).toBeTruthy()
  })
})
