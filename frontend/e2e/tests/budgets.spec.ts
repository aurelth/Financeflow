import { expect } from '@playwright/test'
import { test } from '../fixtures/auth.fixture'
import { BudgetsPage } from '../pages/BudgetsPage'

test.describe('Orçamentos', () => {

    test('deve navegar para a página de orçamentos', async ({ authenticatedPage: page }) => {
        const budgetsPage = new BudgetsPage(page)
        await budgetsPage.goto()
        await budgetsPage.isLoaded()
        await expect(budgetsPage.heading).toBeVisible()
    })

    test('deve abrir modal de novo orçamento', async ({ authenticatedPage: page }) => {
        const budgetsPage = new BudgetsPage(page)
        await budgetsPage.goto()
        await budgetsPage.isLoaded()
        await budgetsPage.openNewModal()
        await expect(budgetsPage.modalHeading).toBeVisible()
    })

    test('deve fechar modal ao clicar em cancelar', async ({ authenticatedPage: page }) => {
        const budgetsPage = new BudgetsPage(page)
        await budgetsPage.goto()
        await budgetsPage.isLoaded()
        await budgetsPage.openNewModal()
        await page.getByRole('button', { name: /cancelar/i }).click()
        await expect(budgetsPage.modalHeading).not.toBeVisible()
    })

    test('deve exibir seletor de período', async ({ authenticatedPage: page }) => {
        const budgetsPage = new BudgetsPage(page)
        await budgetsPage.goto()
        await budgetsPage.isLoaded()        
        await expect(page.locator('span.font-medium.capitalize')).toBeVisible()
    })

})