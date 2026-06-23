import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'unconfigured-chromium',
      testMatch: '**/app.spec.ts',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5172',
      },
    },
    {
      name: 'auth-routes-chromium',
      testMatch: '**/auth-routes.spec.ts',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5174',
      },
    },
  ],
  webServer: [
    {
      command: 'npm run dev:unconfigured',
      url: 'http://localhost:5172',
      reuseExistingServer: !process.env.CI,
    },
    {
      command: 'npm run dev:e2e',
      url: 'http://localhost:5174',
      reuseExistingServer: !process.env.CI,
    },
  ],
})
