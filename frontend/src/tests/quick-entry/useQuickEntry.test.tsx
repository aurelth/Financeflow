import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { AI_QUOTA_KEY, useAiQuota, useParseQuickEntry } from '@/features/quick-entry/api/useQuickEntry'
import type { AiQuota, QuickEntryResult } from '@/features/quick-entry/types/quickEntry.types'

const mockGet  = vi.fn()
const mockPost = vi.fn()

vi.mock('@/lib/axios', () => ({
  default: { get: (...a: unknown[]) => mockGet(...a), post: (...a: unknown[]) => mockPost(...a) },
}))

const quota = (used: number): AiQuota => ({ limit: 30, used, remaining: 30 - used, resetsAtUtc: '2026-11-25T03:00:00Z' })

function setup() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  )
  return { qc, wrapper }
}

describe('useQuickEntry', () => {
  beforeEach(() => vi.clearAllMocks())

  it('useAiQuota busca a cota do dia', async () => {
    mockGet.mockResolvedValue({ data: quota(4) })
    const { wrapper } = setup()

    const { result } = renderHook(() => useAiQuota(), { wrapper })

    await waitFor(() => expect(result.current.data).toEqual(quota(4)))
    expect(mockGet).toHaveBeenCalledWith('/api/quickentry/quota')
  })

  it('envia texto e foto como multipart e atualiza a cota com a resposta', async () => {
    const reply: QuickEntryResult = { drafts: [], quota: quota(5) }
    mockPost.mockResolvedValue({ data: reply })
    mockGet.mockResolvedValue({ data: quota(5) })
    const { qc, wrapper } = setup()
    const photo = new File(['x'], 'cupom.jpg', { type: 'image/jpeg' })

    const { result } = renderHook(() => useParseQuickEntry(), { wrapper })
    await act(() => result.current.mutateAsync({ text: '  café 5  ', image: photo }))

    const [url, body] = mockPost.mock.calls[0]
    expect(url).toBe('/api/quickentry/parse')
    expect(body).toBeInstanceOf(FormData)
    expect((body as FormData).get('text')).toBe('café 5')
    expect((body as FormData).get('image')).toBe(photo)
    expect(qc.getQueryData(AI_QUOTA_KEY)).toEqual(quota(5))
  })

  it('não envia campos vazios', async () => {
    mockPost.mockResolvedValue({ data: { drafts: [], quota: quota(1) } })
    mockGet.mockResolvedValue({ data: quota(1) })
    const { wrapper } = setup()

    const { result } = renderHook(() => useParseQuickEntry(), { wrapper })
    await act(() => result.current.mutateAsync({ text: '   ', image: null }))

    const body = mockPost.mock.calls[0][1] as FormData
    expect(body.has('text')).toBe(false)
    expect(body.has('image')).toBe(false)
  })

  it('reconsulta a cota quando a chamada falha (o servidor pode ter devolvido o uso)', async () => {
    mockPost.mockRejectedValue(new Error('429'))
    mockGet.mockResolvedValue({ data: quota(30) })
    const { wrapper } = setup()

    const quotaHook = renderHook(() => useAiQuota(), { wrapper })
    await waitFor(() => expect(quotaHook.result.current.data).toBeDefined())
    mockGet.mockClear()

    const { result } = renderHook(() => useParseQuickEntry(), { wrapper })
    await act(async () => { await result.current.mutateAsync({ text: 'café' }).catch(() => undefined) })

    await waitFor(() => expect(mockGet).toHaveBeenCalledWith('/api/quickentry/quota'))
  })
})
