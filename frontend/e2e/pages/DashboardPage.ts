import { type Page, type Locator } from '@playwright/test'

export class DashboardPage {
  readonly page:    Page
  readonly heading: Locator

  constructor(page: Page) {
    this.page    = page
    this.heading = page.getByRole('heading', { name: 'Dashboard' })
  }

  async goto() {
    await this.page.goto('/dashboard')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }
}