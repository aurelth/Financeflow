import { expect } from '@playwright/test'
import { test } from '../fixtures/auth.fixture'
import { CategoriesPage } from '../pages/CategoriesPage'

test.describe('Categorias', () => {

    test('deve navegar para a página de categorias', async ({ authenticatedPage: page }) => {
        const categoriesPage = new CategoriesPage(page)
        await categoriesPage.goto()
        await categoriesPage.isLoaded()
        await expect(categoriesPage.heading).toBeVisible()
    })

    test('deve exibir categorias listadas', async ({ authenticatedPage: page }) => {
        const categoriesPage = new CategoriesPage(page)
        await categoriesPage.goto()
        await categoriesPage.isLoaded()
        // Verifica que existe pelo menos um card de categoria visível
        await expect(page.locator('.rounded-xl').first()).toBeVisible()
    })

    test('deve abrir modal de nova categoria', async ({ authenticatedPage: page }) => {
        const categoriesPage = new CategoriesPage(page)
        await categoriesPage.goto()
        await categoriesPage.isLoaded()
        await categoriesPage.openNewModal()
        await expect(categoriesPage.modalHeading).toBeVisible()
    })

    test('deve fechar modal ao clicar em cancelar', async ({ authenticatedPage: page }) => {
        const categoriesPage = new CategoriesPage(page)
        await categoriesPage.goto()
        await categoriesPage.isLoaded()
        await categoriesPage.openNewModal()
        await page.getByRole('button', { name: /cancelar/i }).click()
        await expect(categoriesPage.modalHeading).not.toBeVisible()
    })

    test('deve filtrar categorias por tipo', async ({ authenticatedPage: page }) => {
        const categoriesPage = new CategoriesPage(page)
        await categoriesPage.goto()
        await categoriesPage.isLoaded()

        await page.getByRole('button', { name: /despesas/i }).click()
        await expect(page.getByText('Alimentação')).toBeVisible()
    })

})