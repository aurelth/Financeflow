import { test as base, type Page } from '@playwright/test'
import { LoginPage } from '../pages/LoginPage'

const E2E_EMAIL    = 'e2e@financeflow.com'
const E2E_PASSWORD = 'E2e@Teste123!'

export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ page }, use) => {
    const loginPage = new LoginPage(page)
    await loginPage.goto()
    await loginPage.login(E2E_EMAIL, E2E_PASSWORD)
    await page.waitForURL('**/dashboard')
    await use(page)
  },
})

export { expect } from '@playwright/test'
export { E2E_EMAIL, E2E_PASSWORD }