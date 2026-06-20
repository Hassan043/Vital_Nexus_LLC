import { test, expect } from '@playwright/test'

test.describe('App', () => {
  test('shows Entra configuration instructions without MSAL env vars', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Configure Entra External ID' })).toBeVisible()
    await expect(page.getByText('VITE_B2C_CLIENT_ID')).toBeVisible()
  })

  test('shows create account flow when MSAL env vars are configured', async ({ page }) => {
    const clientId = process.env.VITE_B2C_CLIENT_ID
    const tenantId = process.env.VITE_B2C_TENANT_ID
    test.skip(!clientId || !tenantId, 'MSAL env vars are only set in local dev')

    await page.goto('/create-account')

    await expect(page.getByRole('heading', { name: 'Create your VitalNexus account' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Get started' })).toBeVisible()
  })
})
