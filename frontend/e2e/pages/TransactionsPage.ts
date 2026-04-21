import { type Page, type Locator } from '@playwright/test'

export class TransactionsPage {
  readonly page:           Page
  readonly heading:        Locator
  readonly newButton:      Locator
  readonly modalHeading:   Locator

  constructor(page: Page) {
    this.page         = page
    this.heading      = page.getByRole('heading', { name: 'Transações' })
    this.newButton    = page.getByRole('button', { name: /nova transação/i })
    this.modalHeading = page.getByRole('heading', { name: 'Nova transação' })
  }

  async goto() {
    await this.page.goto('/transactions')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }

  async openNewModal() {
    await this.newButton.click()
    await this.modalHeading.waitFor({ state: 'visible' })
  }
}