import { useState, useRef, useEffect, KeyboardEvent } from 'react'
import { Bot, Send, Trash2, Loader2, Sparkles } from 'lucide-react'
import { useAssistant } from '../api/useAssistant'

const SUGGESTED_QUESTIONS = [
  'Quanto gastei este mês?',
  'Estou dentro do meu orçamento?',
  'Quais são minhas maiores despesas?',
  'Como estão minhas metas financeiras?',
  'Como foram minhas finanças no mês passado?',
]

export default function AssistantPage() {
  const [input, setInput]         = useState('')
  const messagesEndRef             = useRef<HTMLDivElement>(null)
  const inputRef                   = useRef<HTMLTextAreaElement>(null)
  const { messages, isLoading, error, sendMessage, clearMessages } = useAssistant()

  // Scroll automático para a última mensagem
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isLoading])

  async function handleSend() {
    const trimmed = input.trim()
    if (!trimmed || isLoading) return
    setInput('')
    await sendMessage(trimmed)
    inputRef.current?.focus()
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  function handleSuggestion(question: string) {
    setInput(question)
    inputRef.current?.focus()
  }

  const isEmpty = messages.length === 0

  return (
    <div className="flex flex-col h-[calc(100vh-6rem)]">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between mb-6 flex-shrink-0">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Assistente IA
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Tire dúvidas sobre as suas finanças em linguagem natural
          </p>
        </div>
        {!isEmpty && (
          <button
            onClick={clearMessages}
            className="flex items-center gap-2 h-9 px-3 rounded-xl text-sm font-medium transition-colors"
            style={{ color: 'var(--ff-text-muted)', border: '1px solid var(--ff-border)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color       = 'var(--ff-text-primary)'
              e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color       = 'var(--ff-text-muted)'
              e.currentTarget.style.background  = 'transparent'
            }}
          >
            <Trash2 size={15} />
            Limpar conversa
          </button>
        )}
      </div>

      {/* Área de mensagens */}
      <div
        className="flex-1 overflow-y-auto rounded-2xl p-4 space-y-4 mb-4"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >

        {/* Estado inicial — boas-vindas e sugestões */}
        {isEmpty && (
          <div className="flex flex-col items-center justify-center h-full gap-6 py-8">
            <div
              className="w-16 h-16 rounded-2xl flex items-center justify-center"
              style={{ background: 'rgba(16,185,129,0.1)' }}
            >
              <Sparkles size={28} style={{ color: 'var(--ff-emerald)' }} />
            </div>
            <div className="text-center">
              <p className="text-base font-semibold mb-1" style={{ color: 'var(--ff-text-primary)' }}>
                Olá! Sou o seu assistente financeiro.
              </p>
              <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
                Posso analisar os seus dados financeiros e responder perguntas em linguagem natural.
              </p>
            </div>
            <div className="flex flex-wrap gap-2 justify-center max-w-lg">
              {SUGGESTED_QUESTIONS.map(question => (
                <button
                  key={question}
                  onClick={() => handleSuggestion(question)}
                  className="px-3 py-2 rounded-xl text-sm transition-colors"
                  style={{
                    background: 'var(--ff-bg-elevated)',
                    color:      'var(--ff-text-secondary)',
                    border:     '1px solid var(--ff-border)',
                  }}
                  onMouseEnter={e => {
                    e.currentTarget.style.color      = 'var(--ff-emerald)'
                    e.currentTarget.style.background = 'rgba(16,185,129,0.08)'
                    e.currentTarget.style.borderColor = 'rgba(16,185,129,0.3)'
                  }}
                  onMouseLeave={e => {
                    e.currentTarget.style.color       = 'var(--ff-text-secondary)'
                    e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
                    e.currentTarget.style.borderColor = 'var(--ff-border)'
                  }}
                >
                  {question}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Histórico de mensagens */}
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`flex gap-3 ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            {/* Avatar do assistente */}
            {msg.role === 'assistant' && (
              <div
                className="w-8 h-8 rounded-xl flex items-center justify-center flex-shrink-0 mt-0.5"
                style={{ background: 'rgba(16,185,129,0.1)' }}
              >
                <Bot size={16} style={{ color: 'var(--ff-emerald)' }} />
              </div>
            )}

            {/* Bolha da mensagem */}
            <div
              className="max-w-[75%] rounded-2xl px-4 py-3 text-sm leading-relaxed"
              style={msg.role === 'user'
                ? {
                    background: 'var(--ff-emerald)',
                    color:      'var(--ff-emerald-subtle)',
                    borderBottomRightRadius: '4px',
                  }
                : {
                    background: 'var(--ff-bg-elevated)',
                    color:      'var(--ff-text-primary)',
                    border:     '1px solid var(--ff-border)',
                    borderBottomLeftRadius: '4px',
                    whiteSpace: 'pre-wrap',
                  }
              }
            >
              {msg.content}
            </div>
          </div>
        ))}

        {/* Loading — assistente a digitar */}
        {isLoading && (
          <div className="flex gap-3 justify-start">
            <div
              className="w-8 h-8 rounded-xl flex items-center justify-center flex-shrink-0 mt-0.5"
              style={{ background: 'rgba(16,185,129,0.1)' }}
            >
              <Bot size={16} style={{ color: 'var(--ff-emerald)' }} />
            </div>
            <div
              className="rounded-2xl px-4 py-3 flex items-center gap-1.5"
              style={{
                background:            'var(--ff-bg-elevated)',
                border:                '1px solid var(--ff-border)',
                borderBottomLeftRadius: '4px',
              }}
            >
              <Loader2 size={14} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
              <span className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
                A analisar os seus dados...
              </span>
            </div>
          </div>
        )}

        {/* Erro */}
        {error && (
          <div
            className="text-sm text-center py-2 px-4 rounded-xl"
            style={{ background: 'rgba(244,63,94,0.1)', color: 'var(--ff-expense)' }}
          >
            {error}
          </div>
        )}

        {/* Âncora para scroll automático */}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div
        className="flex items-end gap-3 p-3 rounded-2xl flex-shrink-0"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <textarea
          ref={inputRef}
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Escreva a sua pergunta... (Enter para enviar)"
          rows={1}
          disabled={isLoading}
          className="flex-1 resize-none bg-transparent text-sm outline-none leading-relaxed"
          style={{
            color:             'var(--ff-text-primary)',
            maxHeight:         '120px',
            overflowY:         'auto',
            paddingTop:        '6px',
            paddingBottom:     '6px',
          }}
        />
        <button
          onClick={handleSend}
          disabled={!input.trim() || isLoading}
          className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0 transition-all disabled:opacity-40 disabled:cursor-not-allowed"
          style={{ background: 'var(--ff-emerald)' }}
          onMouseEnter={e => {
            if (!e.currentTarget.disabled)
              e.currentTarget.style.background = 'var(--ff-emerald-hover)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.background = 'var(--ff-emerald)'
          }}
        >
          <Send size={16} style={{ color: 'var(--ff-emerald-subtle)' }} />
        </button>
      </div>

    </div>
  )
}