import { describe, it, expect } from 'vitest'
import { getApiErrorMessage } from '@/lib/apiError'

const FALLBACK = 'Erro genérico. Tente novamente.'
const axiosError = (data: unknown) => ({ response: { data } })

describe('getApiErrorMessage', () => {
  it('deve juntar todos os erros de validação em uma mensagem', () => {
    const err = axiosError({ errors: { Name: ['Nome obrigatório.'], Email: ['E-mail inválido.', 'E-mail já usado.'] } })

    expect(getApiErrorMessage(err, FALLBACK)).toBe('Nome obrigatório. E-mail inválido. E-mail já usado.')
  })

  it('deve priorizar os erros de validação sobre a message', () => {
    const err = axiosError({ errors: { Name: ['Nome obrigatório.'] }, message: 'Falha de validação' })

    expect(getApiErrorMessage(err, FALLBACK)).toBe('Nome obrigatório.')
  })

  it('deve usar a message quando não há erros de validação', () => {
    expect(getApiErrorMessage(axiosError({ message: 'Conta já existe.' }), FALLBACK)).toBe('Conta já existe.')
  })

  it('deve ignorar um objeto errors vazio', () => {
    expect(getApiErrorMessage(axiosError({ errors: {}, message: 'Sem detalhes' }), FALLBACK)).toBe('Sem detalhes')
  })

  it('deve usar o fallback quando a resposta não traz mensagem', () => {
    expect(getApiErrorMessage(axiosError({}), FALLBACK)).toBe(FALLBACK)
  })

  it('não deve expor o title técnico do ProblemDetails', () => {
    const err = axiosError({ title: 'An error occurred while processing your request.' })

    expect(getApiErrorMessage(err, FALLBACK)).toBe(FALLBACK)
  })

  it.each([
    ['erro de rede sem response', new Error('Network Error')],
    ['null', null],
    ['undefined', undefined],
    ['string', 'falhou'],
  ])('deve usar o fallback para %s', (_label, err) => {
    expect(getApiErrorMessage(err, FALLBACK)).toBe(FALLBACK)
  })
})
