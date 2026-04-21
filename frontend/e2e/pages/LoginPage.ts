import { type Page, type Locator } from '@playwright/test'

export class LoginPage {
  readonly page:            Page
  readonly emailInput:      Locator
  readonly passwordInput:   Locator
  readonly submitButton:    Locator
  readonly errorMessage:    Locator

  constructor(page: Page) {
    this.page          = page
    this.emailInput    = page.getByPlaceholder('seu@email.com')
    this.passwordInput = page.getByPlaceholder('Sua senha')
    this.submitButton  = page.getByRole('button', { name: /entrar/i })
    this.errorMessage  = page.getByText(/credenciais inválidas/i)
  }

  async goto() {
    await this.page.goto('/login')
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email)
    await this.passwordInput.fill(password)
    await this.submitButton.click()
  }
}