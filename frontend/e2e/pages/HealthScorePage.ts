import { type Page, type Locator } from '@playwright/test'

export class HealthScorePage {
  readonly page:    Page
  readonly heading: Locator

  constructor(page: Page) {
    this.page    = page
    this.heading = page.getByRole('heading', { name: 'Saúde Financeira' })
  }

  async goto() {
    await this.page.goto('/health-score')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }
}