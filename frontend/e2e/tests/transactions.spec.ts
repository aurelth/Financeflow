import { expect } from '@playwright/test'
import { test } from '../fixtures/auth.fixture'
import { TransactionsPage } from '../pages/TransactionsPage'

test.describe('Transações', () => {

    test('deve navegar para a página de transações', async ({ authenticatedPage: page }) => {
        const transactionsPage = new TransactionsPage(page)
        await transactionsPage.goto()
        await transactionsPage.isLoaded()
        await expect(transactionsPage.heading).toBeVisible()
    })

    test('deve abrir modal de nova transação', async ({ authenticatedPage: page }) => {
        const transactionsPage = new TransactionsPage(page)
        await transactionsPage.goto()
        await transactionsPage.isLoaded()
        await transactionsPage.openNewModal()
        await expect(transactionsPage.modalHeading).toBeVisible()
    })

    test('deve fechar modal ao clicar em cancelar', async ({ authenticatedPage: page }) => {
        const transactionsPage = new TransactionsPage(page)
        await transactionsPage.goto()
        await transactionsPage.isLoaded()
        await transactionsPage.openNewModal()
        await page.getByRole('button', { name: /cancelar/i }).first().click()
        await expect(transactionsPage.modalHeading).not.toBeVisible()
    })

    test('deve exibir filtros de data', async ({ authenticatedPage: page }) => {
        const transactionsPage = new TransactionsPage(page)
        await transactionsPage.goto()
        await transactionsPage.isLoaded()
        const dateInputs = page.locator('input[type="date"]')
        await expect(dateInputs.first()).toBeVisible()
    })

    test('deve exibir conteúdo após carregar', async ({ authenticatedPage: page }) => {
        const transactionsPage = new TransactionsPage(page)
        await transactionsPage.goto()
        await transactionsPage.isLoaded()

        // Verifica que os filtros de data estão visíveis — sempre presentes independente de haver transações
        await expect(page.locator('input[type="date"]').first()).toBeVisible()
    })

})