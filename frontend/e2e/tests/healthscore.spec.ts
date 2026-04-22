import { expect } from '@playwright/test'
import { test } from '../fixtures/auth.fixture'
import { HealthScorePage } from '../pages/HealthScorePage'

test.describe('Saúde Financeira', () => {

  test('deve navegar para a página de saúde financeira', async ({ authenticatedPage: page }) => {
    const healthScorePage = new HealthScorePage(page)
    await healthScorePage.goto()
    await healthScorePage.isLoaded()
    await expect(healthScorePage.heading).toBeVisible()
  })

  test('deve exibir o score', async ({ authenticatedPage: page }) => {
    const healthScorePage = new HealthScorePage(page)
    await healthScorePage.goto()
    await healthScorePage.isLoaded()
    await expect(page.getByText(/de 100/i)).toBeVisible()
  })

  test('deve exibir os critérios de avaliação', async ({ authenticatedPage: page }) => {
    const healthScorePage = new HealthScorePage(page)
    await healthScorePage.goto()
    await healthScorePage.isLoaded()    
    await expect(page.getByText('Saldo do mês', { exact: true })).toBeVisible()
    await expect(page.getByText('Controlo de orçamentos', { exact: true })).toBeVisible()
  })

  test('deve exibir o gráfico de evolução', async ({ authenticatedPage: page }) => {
    const healthScorePage = new HealthScorePage(page)
    await healthScorePage.goto()
    await healthScorePage.isLoaded()
    await expect(page.getByText(/evolução dos últimos 6 meses/i)).toBeVisible()
  })

})