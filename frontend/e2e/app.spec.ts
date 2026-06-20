import { test, expect } from '@playwright/test'

test.describe('App', () => {
  test('shows Entra configuration instructions without MSAL env vars', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Configure Entra External ID' })).toBeVisible()
    await expect(page.getByText('VITE_B2C_CLIENT_ID')).toBeVisible()
  })
})
