import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useAssistant } from '@/features/assistant/api/useAssistant'

// Mock do axios
vi.mock('@/lib/axios', () => ({
  default: {
    post: vi.fn(),
  },
}))

import api from '@/lib/axios'
const mockApi = api as unknown as { post: ReturnType<typeof vi.fn> }

describe('useAssistant', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve iniciar com estado vazio', () => {
    const { result } = renderHook(() => useAssistant())

    expect(result.current.messages).toEqual([])
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('deve adicionar mensagem do utilizador imediatamente ao enviar', async () => {
    mockApi.post.mockResolvedValueOnce({ data: { reply: 'Resposta do assistente.' } })

    const { result } = renderHook(() => useAssistant())

    await act(async () => {
      await result.current.sendMessage('Quanto gastei este mês?')
    })

    expect(result.current.messages[0]).toEqual({
      role:    'user',
      content: 'Quanto gastei este mês?',
    })
  })

  it('deve adicionar resposta do assistente após chamada à API', async () => {
    mockApi.post.mockResolvedValueOnce({ data: { reply: 'Você gastou R$ 500,00.' } })

    const { result } = renderHook(() => useAssistant())

    await act(async () => {
      await result.current.sendMessage('Quanto gastei?')
    })

    expect(result.current.messages).toHaveLength(2)
    expect(result.current.messages[1]).toEqual({
      role:    'assistant',
      content: 'Você gastou R$ 500,00.',
    })
  })

  it('deve definir erro quando a API falha', async () => {
    mockApi.post.mockRejectedValueOnce(new Error('Network error'))

    const { result } = renderHook(() => useAssistant())

    await act(async () => {
      await result.current.sendMessage('Pergunta qualquer')
    })

    expect(result.current.error).toBe('Não foi possível obter uma resposta. Tente novamente.')
    // Mensagem do utilizador ainda aparece
    expect(result.current.messages).toHaveLength(1)
  })

  it('deve limpar mensagens ao chamar clearMessages', async () => {
    mockApi.post.mockResolvedValueOnce({ data: { reply: 'Resposta.' } })

    const { result } = renderHook(() => useAssistant())

    await act(async () => {
      await result.current.sendMessage('Olá')
    })

    expect(result.current.messages).toHaveLength(2)

    act(() => {
      result.current.clearMessages()
    })

    expect(result.current.messages).toEqual([])
    expect(result.current.error).toBeNull()
  })

  it('não deve enviar mensagem vazia', async () => {
    const { result } = renderHook(() => useAssistant())

    await act(async () => {
      await result.current.sendMessage('   ')
    })

    expect(mockApi.post).not.toHaveBeenCalled()
    expect(result.current.messages).toHaveLength(0)
  })

  it('deve retornar isLoading false após conclusão', async () => {
    mockApi.post.mockResolvedValueOnce({ data: { reply: 'Ok.' } })

    const { result } = renderHook(() => useAssistant())

    await act(async () => {
      await result.current.sendMessage('Teste')
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })
  })
})