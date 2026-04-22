import { type Page, type Locator } from '@playwright/test'

export class GoalsPage {
  readonly page:           Page
  readonly heading:        Locator
  readonly newButton:      Locator
  readonly modalHeading:   Locator

  constructor(page: Page) {
    this.page         = page
    this.heading      = page.getByRole('heading', { name: 'Metas Financeiras' })
    this.newButton    = page.getByRole('button', { name: /nova meta/i }).first()
    this.modalHeading = page.getByRole('heading', { name: 'Nova meta' })
  }

  async goto() {
    await this.page.goto('/goals')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }

  async openNewModal() {
    await this.newButton.click()
    await this.modalHeading.waitFor({ state: 'visible' })
  }
}