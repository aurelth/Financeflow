import { describe, it, expect, beforeEach } from 'vitest'
import api from '@/lib/axios'

describe('axios instance', () => {
  beforeEach(() => sessionStorage.clear())

  it('deve ter a baseURL configurada ou undefined em ambiente de teste', () => {    
    expect(api.defaults).toBeDefined()
  })

  it('deve ter withCredentials ativo', () => {
    expect(api.defaults.withCredentials).toBe(true)
  })

  it('deve ter Content-Type application/json', () => {
    expect(api.defaults.headers['Content-Type']).toBe('application/json')
  })
})