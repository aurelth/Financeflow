/// <reference types="node" />
import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir:   './e2e',
  globalSetup:   './e2e/setup/global-setup.ts',
  fullyParallel: false, // Sequencial — testes dependem de estado (login, dados criados)
  forbidOnly: !!process.env.CI,
  retries:    process.env.CI ? 2 : 0,
  workers:    1, // Um worker — evita conflitos de dados entre testes
  reporter:   'html',

  use: {
    baseURL:       'http://localhost:3000',    
    trace:         'on-first-retry',
    screenshot:    'only-on-failure',
    video:         'retain-on-failure',
    actionTimeout: 10_000,
  },

  projects: [
    {
      name:  'chromium',
      use:   { ...devices['Desktop Chrome'] },
    },
  ],  
})