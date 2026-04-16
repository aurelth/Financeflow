import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useOnboarding } from '@/hooks/useOnboarding'

describe('useOnboarding', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('deve retornar isVisible como false inicialmente', () => {
    const { result } = renderHook(() => useOnboarding())
    expect(result.current.isVisible).toBe(false)
  })

  it('deve retornar hasSeenOnboarding false quando localStorage está vazio', () => {
    const { result } = renderHook(() => useOnboarding())
    expect(result.current.hasSeenOnboarding()).toBe(false)
  })

  it('deve retornar hasSeenOnboarding true quando flag existe no localStorage', () => {
    localStorage.setItem('onboarding_seen', 'true')
    const { result } = renderHook(() => useOnboarding())
    expect(result.current.hasSeenOnboarding()).toBe(true)
  })

  it('deve definir isVisible como true ao chamar startTour', () => {
    const { result } = renderHook(() => useOnboarding())

    act(() => {
      result.current.startTour()
    })

    expect(result.current.isVisible).toBe(true)
  })

  it('deve gravar flag no localStorage e definir isVisible false ao chamar markAsSeen', () => {
    const { result } = renderHook(() => useOnboarding())

    act(() => {
      result.current.startTour()
    })

    act(() => {
      result.current.markAsSeen()
    })

    expect(result.current.isVisible).toBe(false)
    expect(localStorage.getItem('onboarding_seen')).toBe('true')
  })

  it('deve remover flag do localStorage e definir isVisible true ao chamar resetTour', () => {
    localStorage.setItem('onboarding_seen', 'true')
    const { result } = renderHook(() => useOnboarding())

    act(() => {
      result.current.resetTour()
    })

    expect(result.current.isVisible).toBe(true)
    expect(localStorage.getItem('onboarding_seen')).toBeNull()
  })

  it('deve iniciar tour ao chamar startIfFirstVisit quando não viu onboarding', () => {
    const { result } = renderHook(() => useOnboarding())

    act(() => {
      result.current.startIfFirstVisit()
    })

    expect(result.current.isVisible).toBe(true)
  })

  it('não deve iniciar tour ao chamar startIfFirstVisit quando já viu onboarding', () => {
    localStorage.setItem('onboarding_seen', 'true')
    const { result } = renderHook(() => useOnboarding())

    act(() => {
      result.current.startIfFirstVisit()
    })

    expect(result.current.isVisible).toBe(false)
  })
})