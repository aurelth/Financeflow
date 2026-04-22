import { expect } from '@playwright/test'
import { test } from '../fixtures/auth.fixture'
import { AssistantPage } from '../pages/AssistantPage'

test.describe('Assistente IA', () => {

  test('deve navegar para a página do assistente', async ({ authenticatedPage: page }) => {
    const assistantPage = new AssistantPage(page)
    await assistantPage.goto()
    await assistantPage.isLoaded()
    await expect(assistantPage.heading).toBeVisible()
  })

  test('deve exibir mensagem de boas-vindas', async ({ authenticatedPage: page }) => {
    const assistantPage = new AssistantPage(page)
    await assistantPage.goto()
    await assistantPage.isLoaded()
    await expect(page.getByText(/sou o seu assistente financeiro/i)).toBeVisible()
  })

  test('deve exibir sugestões de perguntas', async ({ authenticatedPage: page }) => {
    const assistantPage = new AssistantPage(page)
    await assistantPage.goto()
    await assistantPage.isLoaded()
    await expect(page.getByText('Quanto gastei este mês?')).toBeVisible()
  })

  test('deve preencher o input ao clicar numa sugestão', async ({ authenticatedPage: page }) => {
    const assistantPage = new AssistantPage(page)
    await assistantPage.goto()
    await assistantPage.isLoaded()

    await page.getByText('Quanto gastei este mês?').click()
    await expect(assistantPage.input).toHaveValue('Quanto gastei este mês?')
  })

  test('deve enviar mensagem e receber resposta', async ({ authenticatedPage: page }) => {
    const assistantPage = new AssistantPage(page)
    await assistantPage.goto()
    await assistantPage.isLoaded()

    await assistantPage.sendMessage('Qual é o meu saldo atual?')

    // Verifica que a mensagem do utilizador aparece
    await expect(page.getByText('Qual é o meu saldo atual?')).toBeVisible()

    // Aguarda a resposta do assistente (pode demorar)
    await expect(page.locator('.rounded-2xl').filter({ hasText: /R\$|saldo|mês/i }).last())
      .toBeVisible({ timeout: 30000 })
  })

})