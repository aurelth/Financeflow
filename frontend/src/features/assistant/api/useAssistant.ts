import { useState, useCallback } from 'react'
import api from '@/lib/axios'

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

interface SendMessageResponse {
  reply: string
}

export function useAssistant() {
  const [messages, setMessages]   = useState<ChatMessage[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError]         = useState<string | null>(null)

  const sendMessage = useCallback(async (message: string) => {
    if (!message.trim() || isLoading) return

    // Adiciona a mensagem do utilizador ao histórico imediatamente
    const userMessage: ChatMessage = { role: 'user', content: message }
    setMessages(prev => [...prev, userMessage])
    setIsLoading(true)
    setError(null)

    try {
      const { data } = await api.post<SendMessageResponse>('/assistant/chat', { message })

      const assistantMessage: ChatMessage = {
        role:    'assistant',
        content: data.reply,
      }
      setMessages(prev => [...prev, assistantMessage])
    } catch {
      setError('Não foi possível obter uma resposta. Tente novamente.')
    } finally {
      setIsLoading(false)
    }
  }, [isLoading])

  const clearMessages = useCallback(() => {
    setMessages([])
    setError(null)
  }, [])

  return { messages, isLoading, error, sendMessage, clearMessages }
}