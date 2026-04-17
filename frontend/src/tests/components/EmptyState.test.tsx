import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Receipt } from 'lucide-react'
import EmptyState from '@/components/ui/EmptyState'

describe('EmptyState', () => {
  it('deve renderizar título e descrição', () => {
    render(
      <EmptyState
        icon={Receipt}
        title="Nenhuma transação"
        description="Cria a tua primeira transação"
      />
    )
    expect(screen.getByText('Nenhuma transação')).toBeInTheDocument()
    expect(screen.getByText('Cria a tua primeira transação')).toBeInTheDocument()
  })

  it('deve renderizar botão de acção quando fornecido', () => {
    const mockClick = vi.fn()
    render(
      <EmptyState
        icon={Receipt}
        title="Nenhuma transação"
        description="Cria a tua primeira transação"
        action={{ label: 'Criar transação', onClick: mockClick }}
      />
    )
    expect(screen.getByRole('button', { name: /criar transação/i })).toBeInTheDocument()
  })

  it('não deve renderizar botão quando action não é fornecido', () => {
    render(
      <EmptyState
        icon={Receipt}
        title="Nenhuma transação"
        description="Cria a tua primeira transação"
      />
    )
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('deve chamar onClick ao clicar no botão de acção', async () => {
    const mockClick = vi.fn()
    const user = userEvent.setup()

    render(
      <EmptyState
        icon={Receipt}
        title="Nenhuma transação"
        description="Cria a tua primeira transação"
        action={{ label: 'Criar transação', onClick: mockClick }}
      />
    )

    await user.click(screen.getByRole('button', { name: /criar transação/i }))
    expect(mockClick).toHaveBeenCalledOnce()
  })
})