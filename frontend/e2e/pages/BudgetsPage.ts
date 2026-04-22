import { type Page, type Locator } from '@playwright/test'

export class BudgetsPage {
  readonly page:           Page
  readonly heading:        Locator
  readonly newButton:      Locator
  readonly modalHeading:   Locator

  constructor(page: Page) {
    this.page         = page
    this.heading      = page.getByRole('heading', { name: 'Orçamentos' })
    this.newButton    = page.getByRole('button', { name: /novo orçamento/i })
    this.modalHeading = page.getByRole('heading', { name: 'Novo orçamento' })
  }

  async goto() {
    await this.page.goto('/budgets')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }

  async openNewModal() {
    await this.newButton.click()
    await this.modalHeading.waitFor({ state: 'visible' })
  }
}