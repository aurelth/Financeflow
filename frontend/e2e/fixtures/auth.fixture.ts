import { test as base, type Page } from '@playwright/test'
import * as fs from 'fs'

export const E2E_EMAIL    = 'e2e@financeflow.com'
export const E2E_PASSWORD = 'E2e@Teste123!'

export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ browser }, use) => {
    const sessionData = JSON.parse(fs.readFileSync('e2e/.auth/session.json', 'utf-8'))

    const context = await browser.newContext({ baseURL: 'http://localhost:3000' })
    const page    = await context.newPage()

    // Navega para a app e injeta o sessionStorage antes do React carregar
    await page.goto('/')
    await page.evaluate((data) => {
      if (data.accessToken) sessionStorage.setItem('accessToken', data.accessToken)
      if (data.user)        sessionStorage.setItem('user', data.user)
    }, sessionData)

    // Recarrega para o React ler o sessionStorage
    await page.reload()
    await page.waitForURL('**/dashboard', { timeout: 15000 })

    await use(page)
    await context.close()
  },
})

export { expect } from '@playwright/test'