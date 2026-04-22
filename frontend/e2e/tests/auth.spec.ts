import { test as base, expect } from '@playwright/test'
import { LoginPage }     from '../pages/LoginPage'
import { DashboardPage } from '../pages/DashboardPage'
import { E2E_EMAIL, E2E_PASSWORD } from '../fixtures/auth.fixture'

// Testes de auth não usam storageState — testam o login em si
const test = base.extend({})
test.use({ storageState: { cookies: [], origins: [] } })

test.describe('Autenticação', () => {

  test('deve fazer login com credenciais válidas', async ({ page }) => {
    const loginPage     = new LoginPage(page)
    const dashboardPage = new DashboardPage(page)

    await loginPage.goto()
    await loginPage.login(E2E_EMAIL, E2E_PASSWORD)

    await page.waitForURL('**/dashboard')
    await dashboardPage.isLoaded()

    await expect(dashboardPage.heading).toBeVisible()
  })

  test('deve exibir erro com credenciais inválidas', async ({ page }) => {
    const loginPage = new LoginPage(page)

    await loginPage.goto()
    await loginPage.login('invalido@teste.com', 'senhaerrada')

    await page.waitForTimeout(2000)
    await expect(page).toHaveURL(/login/)
  })

  test('deve redirecionar para login quando não autenticado', async ({ page }) => {
    await page.goto('/dashboard')
    await expect(page).toHaveURL(/login/)
  })

  test('deve fazer logout com sucesso', async ({ page }) => {
    const loginPage = new LoginPage(page)

    await loginPage.goto()
    await page.waitForLoadState('networkidle')
    await loginPage.login(E2E_EMAIL, E2E_PASSWORD)

    await page.waitForURL('**/dashboard', { timeout: 30000 })
    await page.waitForSelector('[data-sonner-toast]', { state: 'detached', timeout: 10000 })

    await page.getByRole('button', { name: /E2E Teste/i }).click()
    await page.waitForSelector('text=Sair', { state: 'visible', timeout: 5000 })
    await page.getByText('Sair').click()

    await expect(page).toHaveURL(/login/)
  })

})