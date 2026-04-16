import { useState, useCallback } from 'react'

const ONBOARDING_KEY = 'onboarding_seen'

export function useOnboarding() {
  const [isVisible, setIsVisible] = useState(false)

  // Verifica se o utilizador já viu o onboarding
  const hasSeenOnboarding = (): boolean => {
    try {
      return localStorage.getItem(ONBOARDING_KEY) === 'true'
    } catch {
      return false
    }
  }

  // Marca o onboarding como visto
  const markAsSeen = useCallback(() => {
    try {
      localStorage.setItem(ONBOARDING_KEY, 'true')
    } catch {
      // localStorage indisponível — ignora silenciosamente
    }
    setIsVisible(false)
  }, [])

  // Inicia o tour
  const startTour = useCallback(() => {
    setIsVisible(true)
  }, [])

  // Reinicia o tour (usado nas Configurações)
  const resetTour = useCallback(() => {
    try {
      localStorage.removeItem(ONBOARDING_KEY)
    } catch {
      // localStorage indisponível — ignora silenciosamente
    }
    setIsVisible(true)
  }, [])

  // Inicia automaticamente se for o primeiro acesso
  const startIfFirstVisit = useCallback(() => {
    if (!hasSeenOnboarding()) {
      setIsVisible(true)
    }
  }, [])

  return {
    isVisible,
    hasSeenOnboarding,
    startTour,
    resetTour,
    markAsSeen,
    startIfFirstVisit,
  }
}