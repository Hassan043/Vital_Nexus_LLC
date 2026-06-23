import { test, expect } from '@playwright/test'

test.describe('Entra External ID auth routes', () => {
  test('redirects protected home routes to sign-in', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/\/sign-in$/)
    await expect(page.getByRole('heading', { name: 'Sign in to VitalNexus' })).toBeVisible()
    await expect(page.getByText('Sign in to continue to')).toBeVisible()
    await expect(page.locator('.auth-status strong')).toHaveText('/')
  })

  test('shows the create account wizard for guest routes', async ({ page }) => {
    await page.goto('/create-account')

    await expect(page.getByRole('heading', { name: 'Create your VitalNexus account' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Get started' })).toBeVisible()
  })

  test('shows optional email sign-in controls on the guest sign-in page', async ({ page }) => {
    await page.goto('/sign-in')

    await expect(page.getByLabel('Email address (optional)')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Create an account' })).toBeVisible()
  })
})
