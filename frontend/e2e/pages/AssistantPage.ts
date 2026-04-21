import { type Page, type Locator } from '@playwright/test'

export class AssistantPage {
  readonly page:        Page
  readonly heading:     Locator
  readonly input:       Locator
  readonly sendButton:  Locator

  constructor(page: Page) {
    this.page       = page
    this.heading    = page.getByRole('heading', { name: 'Assistente IA' })
    this.input      = page.getByPlaceholder(/escreva a sua pergunta/i)
    this.sendButton = page.locator('button').filter({ has: page.locator('svg.lucide-send') })
  }

  async goto() {
    await this.page.goto('/assistant')
  }

  async isLoaded() {
    await this.heading.waitFor({ state: 'visible' })
  }

  async sendMessage(message: string) {
    await this.input.fill(message)
    await this.sendButton.click()
  }
}