import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import QuickEntryDialog from '@/features/quick-entry/components/QuickEntryDialog'
import type { AiQuota, QuickEntryDraft, QuickEntryResult } from '@/features/quick-entry/types/quickEntry.types'

const mockMutate = vi.fn()
let mockQuota: AiQuota | undefined
let mockPending = false

vi.mock('@/features/quick-entry/api/useQuickEntry', () => ({
  useAiQuota: () => ({ data: mockQuota }),
  useParseQuickEntry: () => ({ mutate: mockMutate, isPending: mockPending }),
}))

const draft: QuickEntryDraft = {
  type: 'Expense', amount: 45.9, date: '2026-11-23T00:00:00', description: 'Almoço iFood',
  categoryId: 'cat-1', categoryName: 'Restaurantes',
}

const quota = (used: number, limit = 30): AiQuota => ({
  limit, used, remaining: limit - used, resetsAtUtc: '2026-11-25T03:00:00Z',
})

// Faz o mutate chamar o callback como o React Query chamaria
function resolveWith(result: QuickEntryResult) {
  mockMutate.mockImplementation((_vars, opts) => opts?.onSuccess?.(result))
}

function rejectWith(message: string | undefined) {
  mockMutate.mockImplementation((_vars, opts) =>
    opts?.onError?.({ response: { data: message ? { message } : {} } }))
}

const renderDialog = () => {
  const onClose = vi.fn()
  const onResult = vi.fn()
  render(<QuickEntryDialog onClose={onClose} onResult={onResult} />)
  return { onClose, onResult }
}

describe('QuickEntryDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockQuota = quota(2)
    mockPending = false
  })

  it('mostra quantos usos restam hoje', () => {
    renderDialog()
    expect(screen.getByTestId('quick-entry-quota')).toHaveTextContent('quickEntry.quotaRemaining')
  })

  it('só habilita "Interpretar" quando há texto ou foto', async () => {
    renderDialog()
    const user = userEvent.setup()
    const submit = screen.getByRole('button', { name: 'quickEntry.submit' })

    expect(submit).toBeDisabled()
    await user.type(screen.getByLabelText('quickEntry.textLabel'), '   ')
    expect(submit).toBeDisabled()
    await user.type(screen.getByLabelText('quickEntry.textLabel'), 'café 5')
    expect(submit).toBeEnabled()
  })

  it('envia o texto e entrega os rascunhos ao concluir', async () => {
    resolveWith({ drafts: [draft], quota: quota(3) })
    const { onResult } = renderDialog()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('quickEntry.textLabel'), 'almoço 45,90 ontem')
    await user.click(screen.getByRole('button', { name: 'quickEntry.submit' }))

    expect(mockMutate).toHaveBeenCalledWith({ text: 'almoço 45,90 ontem', image: null }, expect.anything())
    expect(onResult).toHaveBeenCalledWith({ drafts: [draft], image: null })
  })

  it('avisa quando a IA não encontra nenhum lançamento e não fecha o diálogo', async () => {
    resolveWith({ drafts: [], quota: quota(3) })
    const { onResult } = renderDialog()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('quickEntry.textLabel'), 'bom dia')
    await user.click(screen.getByRole('button', { name: 'quickEntry.submit' }))

    expect(screen.getByRole('status')).toHaveTextContent('quickEntry.nothingFound')
    expect(onResult).not.toHaveBeenCalled()
  })

  it('mostra a mensagem de erro do servidor (ex.: limite diário)', async () => {
    rejectWith('Você atingiu o limite diário de 30 lançamentos por IA.')
    const { onResult } = renderDialog()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('quickEntry.textLabel'), 'café 5')
    await user.click(screen.getByRole('button', { name: 'quickEntry.submit' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Você atingiu o limite diário de 30 lançamentos por IA.')
    expect(onResult).not.toHaveBeenCalled()
  })

  it('cai na mensagem genérica quando o erro não traz mensagem', async () => {
    rejectWith(undefined)
    renderDialog()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('quickEntry.textLabel'), 'café 5')
    await user.click(screen.getByRole('button', { name: 'quickEntry.submit' }))

    expect(screen.getByRole('alert')).toHaveTextContent('quickEntry.genericError')
  })

  it('bloqueia o envio quando a cota do dia acabou', async () => {
    mockQuota = quota(30)
    renderDialog()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('quickEntry.textLabel'), 'café 5')

    expect(screen.getByTestId('quick-entry-quota')).toHaveTextContent('quickEntry.quotaExhausted')
    expect(screen.getByRole('button', { name: 'quickEntry.submit' })).toBeDisabled()
  })

  it('aceita uma foto, mostra o nome e permite removê-la', async () => {
    renderDialog()
    const user = userEvent.setup()
    const photo = new File(['x'], 'cupom.jpg', { type: 'image/jpeg' })

    await user.upload(screen.getByTestId('quick-entry-file'), photo)

    await waitFor(() => expect(screen.getByTestId('quick-entry-image-name')).toHaveTextContent('cupom.jpg'))
    expect(screen.getByRole('button', { name: 'quickEntry.submit' })).toBeEnabled()

    await user.click(screen.getByRole('button', { name: 'quickEntry.removePhoto' }))
    expect(screen.queryByTestId('quick-entry-image-name')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'quickEntry.submit' })).toBeDisabled()
  })

  it('envia a foto junto e a devolve com os rascunhos', async () => {
    resolveWith({ drafts: [draft], quota: quota(3) })
    const { onResult } = renderDialog()
    const user = userEvent.setup()
    const photo = new File(['x'], 'cupom.jpg', { type: 'image/jpeg' })

    await user.upload(screen.getByTestId('quick-entry-file'), photo)
    await waitFor(() => expect(screen.getByTestId('quick-entry-image-name')).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'quickEntry.submit' }))

    expect(mockMutate).toHaveBeenCalledWith({ text: '', image: photo }, expect.anything())
    expect(onResult).toHaveBeenCalledWith({ drafts: [draft], image: photo })
  })

  it('recusa foto acima de 5 MB sem enviar nada', async () => {
    renderDialog()
    const huge = new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'enorme.jpg', { type: 'image/jpeg' })

    fireEvent.change(screen.getByTestId('quick-entry-file'), { target: { files: [huge] } })

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('quickEntry.imageTooLarge'))
    expect(screen.queryByTestId('quick-entry-image-name')).not.toBeInTheDocument()
    expect(mockMutate).not.toHaveBeenCalled()
  })

  it('desabilita o envio enquanto a IA lê', async () => {
    mockPending = true
    renderDialog()

    expect(screen.getByRole('button', { name: 'quickEntry.reading' })).toBeDisabled()
  })

  it('fecha pelo X e por Cancelar', async () => {
    const { onClose } = renderDialog()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'quickEntry.close' }))
    await user.click(screen.getByRole('button', { name: 'quickEntry.cancel' }))

    expect(onClose).toHaveBeenCalledTimes(2)
  })
})
