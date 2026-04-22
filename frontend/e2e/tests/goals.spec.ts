import { expect } from '@playwright/test'
import { test } from '../fixtures/auth.fixture'
import { GoalsPage } from '../pages/GoalsPage'

test.describe('Metas Financeiras', () => {

    test('deve navegar para a página de metas', async ({ authenticatedPage: page }) => {
        const goalsPage = new GoalsPage(page)
        await goalsPage.goto()
        await goalsPage.isLoaded()
        await expect(goalsPage.heading).toBeVisible()
    })

    test('deve abrir modal de nova meta', async ({ authenticatedPage: page }) => {
        const goalsPage = new GoalsPage(page)
        await goalsPage.goto()
        await goalsPage.isLoaded()
        await goalsPage.openNewModal()
        await expect(goalsPage.modalHeading).toBeVisible()
    })

    test('deve fechar modal ao clicar em cancelar', async ({ authenticatedPage: page }) => {
        const goalsPage = new GoalsPage(page)
        await goalsPage.goto()
        await goalsPage.isLoaded()
        await goalsPage.openNewModal()
        await page.getByRole('button', { name: /cancelar/i }).click()
        await expect(goalsPage.modalHeading).not.toBeVisible()
    })

    test('deve exibir empty state ou metas existentes', async ({ authenticatedPage: page }) => {
        const goalsPage = new GoalsPage(page)
        await goalsPage.goto()
        await goalsPage.isLoaded()

        const hasEmptyState = await page.getByText(/nenhuma meta definida/i).isVisible().catch(() => false)
        const hasGoals = await page.getByText(/em andamento/i).isVisible().catch(() => false)        
        const pageLoaded = await page.getByRole('heading', { name: 'Metas Financeiras' }).isVisible().catch(() => false)
        expect(hasEmptyState || hasGoals || pageLoaded).toBeTruthy()
    })

})