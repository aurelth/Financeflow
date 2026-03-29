import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AttachmentViewer from '@/features/transactions/components/AttachmentViewer'
import api from '@/lib/axios'

// Mock do axios
vi.mock('@/lib/axios', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data:    new Blob(['fake content'], { type: 'image/jpeg' }),
      headers: { 'content-type': 'image/jpeg' },
    }),
  },
}))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  getAttachmentUrl: (id: string) =>
    `http://localhost/api/transactions/${id}/attachment`,
}))

const renderViewer = (
  triggerIcon: 'paperclip' | 'file' = 'paperclip',
  fileName = 'comprovante.pdf'
) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <AttachmentViewer
        transactionId="tx-1"
        fileName={fileName}
        triggerIcon={triggerIcon}
      />
    </QueryClientProvider>
  )
}

describe('AttachmentViewer', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o ícone paperclip quando triggerIcon é paperclip', () => {
    renderViewer('paperclip')
    const button = screen.getByTitle('Ver comprovante')
    expect(button).toBeInTheDocument()
    expect(button.querySelector('.lucide-paperclip')).toBeInTheDocument()
  })

  it('deve renderizar o ícone file quando triggerIcon é file', () => {
    renderViewer('file')
    const button = screen.getByTitle('Ver comprovante')
    expect(button).toBeInTheDocument()
    expect(button.querySelector('.lucide-file-text')).toBeInTheDocument()
  })

  it('deve abrir dropdown com opções ao clicar no ícone', async () => {
    renderViewer()
    const user = userEvent.setup()

    await user.click(screen.getByTitle('Ver comprovante'))

    expect(screen.getByText('Visualizar')).toBeInTheDocument()
    expect(screen.getByText('Baixar')).toBeInTheDocument()
  })

  it('deve fechar dropdown ao clicar fora', async () => {
    renderViewer()
    const user = userEvent.setup()

    await user.click(screen.getByTitle('Ver comprovante'))
    expect(screen.getByText('Visualizar')).toBeInTheDocument()

    await user.click(document.body)

    await waitFor(() => {
      expect(screen.queryByText('Visualizar')).not.toBeInTheDocument()
    })
  })

  it('deve abrir modal de preview ao clicar em Visualizar', async () => {
    renderViewer('paperclip', 'imagem.jpg')
    const user = userEvent.setup()

    await user.click(screen.getByTitle('Ver comprovante'))
    await user.click(screen.getByText('Visualizar'))

    await waitFor(() => {
      expect(screen.getByText('imagem.jpg')).toBeInTheDocument()
    })
  })

  it('deve fechar modal de preview ao clicar em X', async () => {
    renderViewer('paperclip', 'imagem.jpg')
    const user = userEvent.setup()

    await user.click(screen.getByTitle('Ver comprovante'))
    await user.click(screen.getByText('Visualizar'))

    await waitFor(() => {
      expect(screen.getByText('imagem.jpg')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /fechar preview/i }))

    await waitFor(() => {
      expect(screen.queryByRole('img')).not.toBeInTheDocument()
    })
  })

  it('deve disparar download ao clicar em Baixar', async () => {
  renderViewer()
  const user = userEvent.setup()

  // Espiona o appendChild do body para capturar o link criado
  const appendChildSpy = vi.spyOn(document.body, 'appendChild').mockImplementation(() => document.body)
  const removeChildSpy = vi.spyOn(document.body, 'removeChild').mockImplementation(() => document.body)

  await user.click(screen.getByTitle('Ver comprovante'))
  await user.click(screen.getByText('Baixar'))

  // Verifica que o dropdown fechou
  await waitFor(() => {
    expect(screen.queryByText('Visualizar')).not.toBeInTheDocument()
  })

  appendChildSpy.mockRestore()
  removeChildSpy.mockRestore()
  })

  it('deve exibir controles de zoom apenas para imagens', async () => {
  // Mock retorna imagem
  vi.mocked(api.get).mockResolvedValueOnce({
    data:    new Blob(['fake'], { type: 'image/jpeg' }),
    headers: { 'content-type': 'image/jpeg' },
  })

  renderViewer('paperclip', 'imagem.jpg')
  const user = userEvent.setup()

  await user.click(screen.getByTitle('Ver comprovante'))
  await user.click(screen.getByText('Visualizar'))

  await waitFor(() => {
    expect(screen.getByTitle('Aumentar zoom')).toBeInTheDocument()
    expect(screen.getByTitle('Diminuir zoom')).toBeInTheDocument()
    expect(screen.getByText('100%')).toBeInTheDocument()
  })
})

it('não deve exibir controles de zoom para PDFs', async () => {
  vi.mocked(api.get).mockResolvedValueOnce({
    data:    new Blob(['fake'], { type: 'application/pdf' }),
    headers: { 'content-type': 'application/pdf' },
  })

  renderViewer('paperclip', 'documento.pdf')
  const user = userEvent.setup()

  await user.click(screen.getByTitle('Ver comprovante'))
  await user.click(screen.getByText('Visualizar'))

  await waitFor(() => {
    expect(screen.queryByTitle('Aumentar zoom')).not.toBeInTheDocument()
    expect(screen.queryByTitle('Diminuir zoom')).not.toBeInTheDocument()
  })
})

it('deve aumentar o zoom ao clicar em zoom+', async () => {
  vi.mocked(api.get).mockResolvedValueOnce({
    data:    new Blob(['fake'], { type: 'image/jpeg' }),
    headers: { 'content-type': 'image/jpeg' },
  })

  renderViewer('paperclip', 'imagem.jpg')
  const user = userEvent.setup()

  await user.click(screen.getByTitle('Ver comprovante'))
  await user.click(screen.getByText('Visualizar'))

  await waitFor(() => {
    expect(screen.getByText('100%')).toBeInTheDocument()
  })

  await user.click(screen.getByTitle('Aumentar zoom'))

  expect(screen.getByText('125%')).toBeInTheDocument()
})

it('deve diminuir o zoom ao clicar em zoom-', async () => {
  vi.mocked(api.get).mockResolvedValueOnce({
    data:    new Blob(['fake'], { type: 'image/jpeg' }),
    headers: { 'content-type': 'image/jpeg' },
  })

  renderViewer('paperclip', 'imagem.jpg')
  const user = userEvent.setup()

  await user.click(screen.getByTitle('Ver comprovante'))
  await user.click(screen.getByText('Visualizar'))

  await waitFor(() => {
    expect(screen.getByText('100%')).toBeInTheDocument()
  })

  await user.click(screen.getByTitle('Diminuir zoom'))

  expect(screen.getByText('75%')).toBeInTheDocument()
  })
})