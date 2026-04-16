import { describe, it, expect } from 'vitest'
import { getTourSteps } from '@/features/onboarding/steps/tourSteps'

// Mock simples da função t
const t = (key: string) => key

describe('getTourSteps', () => {
  it('deve retornar 5 passos', () => {
    const steps = getTourSteps(t)
    expect(steps).toHaveLength(5)
  })

  it('primeiro passo não deve ter elemento alvo (popover centrado)', () => {
    const steps = getTourSteps(t)
    expect(steps[0].element).toBeUndefined()
  })

  it('passos 2 a 5 devem ter elementos alvo', () => {
    const steps = getTourSteps(t)
    expect(steps[1].element).toBe('a[href="/dashboard"]')
    expect(steps[2].element).toBe('a[href="/transactions"]')
    expect(steps[3].element).toBe('a[href="/categories"]')
    expect(steps[4].element).toBe('a[href="/budgets"]')
  })

  it('todos os passos devem ter título e descrição', () => {
    const steps = getTourSteps(t)
    steps.forEach(step => {
      expect(step.popover?.title).toBeTruthy()
      expect(step.popover?.description).toBeTruthy()
    })
  })

  it('passos 2 a 5 devem ter side right', () => {
    const steps = getTourSteps(t)
    steps.slice(1).forEach(step => {
      expect(step.popover?.side).toBe('right')
    })
  })
})