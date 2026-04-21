import { type Page, type Locator } from '@playwright/test'

export class CategoriesPage {
  readonly page:           Page
  readonly heading:        Locator
  readonly newButton:      Locator
  readonly modalHeading:   Locator

  constructor(page: Page) {
    this.page         = page
    this.heading      = page.getByRole('heading', { name: 'Categorias' })
    this.newButton    = page.getByRole('button', { name: /nova categoria/i })
    this.modalHeading = page.getByRole('heading', { name: 'Nova categoria' })
  }

  async goto() {
    await this.page.goto('/categories')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }

  async openNewModal() {
    await this.newButton.click()
    await this.modalHeading.waitFor({ state: 'visible' })
  }
}