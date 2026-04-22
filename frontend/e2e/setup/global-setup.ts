import { chromium } from '@playwright/test'
import { LoginPage } from '../pages/LoginPage'
import * as fs from 'fs'

const BASE_URL     = 'http://localhost:3000'
const E2E_EMAIL    = 'e2e@financeflow.com'
const E2E_PASSWORD = 'E2e@Teste123!'

async function globalSetup() {
  const browser   = await chromium.launch()
  const context   = await browser.newContext({ baseURL: BASE_URL })
  const page      = await context.newPage()
  const loginPage = new LoginPage(page)

  await loginPage.goto()
  await page.waitForLoadState('networkidle')
  await loginPage.login(E2E_EMAIL, E2E_PASSWORD)
  await page.waitForURL('**/dashboard', { timeout: 30000 })

  // Guarda o sessionStorage manualmente
  const sessionData = await page.evaluate(() => {
    return {
      accessToken: sessionStorage.getItem('accessToken'),
      user:        sessionStorage.getItem('user'),
    }
  })

  fs.writeFileSync('e2e/.auth/session.json', JSON.stringify(sessionData))
  await browser.close()
}

export default globalSetup