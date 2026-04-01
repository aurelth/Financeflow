import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import MonthYearPicker from '@/components/ui/MonthYearPicker'

const renderPicker = (props: {
  month:     number
  year:      number
  onChange:  (month: number, year: number) => void
  maxMonth?: number
  maxYear?:  number
}) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <MonthYearPicker {...props} />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('MonthYearPicker', () => {
  it('deve renderizar o mês e ano atual', () => {
    const onChange = vi.fn()
    renderPicker({ month: 3, year: 2026, onChange })
    expect(screen.getByText('Março 2026')).toBeInTheDocument()
  })

  it('deve abrir o picker ao clicar no label', async () => {
    const onChange = vi.fn()
    renderPicker({ month: 3, year: 2026, onChange })
    const user = userEvent.setup()

    await user.click(screen.getByText('Março 2026'))

    await waitFor(() => {
      expect(screen.getByText('Jan')).toBeInTheDocument()
      expect(screen.getByText('Fev')).toBeInTheDocument()
      expect(screen.getByText('Mar')).toBeInTheDocument()
    })
  })

  it('deve fechar o picker ao selecionar um mês', async () => {
    const onChange = vi.fn()
    renderPicker({ month: 3, year: 2026, onChange })
    const user = userEvent.setup()

    await user.click(screen.getByText('Março 2026'))
    await user.click(screen.getByText('Jan'))

    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith(1, 2026)
    })
  })

  it('deve navegar para o mês anterior ao clicar na seta esquerda', async () => {
    const onChange = vi.fn()
    renderPicker({ month: 3, year: 2026, onChange })
    const user = userEvent.setup()

    const buttons = screen.getAllByRole('button')
    await user.click(buttons[0]) // ChevronLeft

    expect(onChange).toHaveBeenCalledWith(2, 2026)
  })

  it('deve navegar para o mês seguinte ao clicar na seta direita', async () => {
    const onChange = vi.fn()
    renderPicker({ month: 3, year: 2026, onChange })
    const user = userEvent.setup()

    const buttons = screen.getAllByRole('button')
    await user.click(buttons[buttons.length - 1]) // ChevronRight

    expect(onChange).toHaveBeenCalledWith(4, 2026)
  })

  it('deve bloquear meses futuros quando maxMonth e maxYear definidos', async () => {
    const onChange = vi.fn()
    const now      = new Date()
    renderPicker({
      month:    now.getMonth() + 1,
      year:     now.getFullYear(),
      onChange,
      maxMonth: now.getMonth() + 1,
      maxYear:  now.getFullYear(),
    })

    const buttons      = screen.getAllByRole('button')
    const rightButton  = buttons[buttons.length - 1]
    expect(rightButton).toBeDisabled()
  })

  it('deve navegar entre anos no picker', async () => {
    const onChange = vi.fn()
    renderPicker({ month: 3, year: 2026, onChange })
    const user = userEvent.setup()

    await user.click(screen.getByText('Março 2026'))

    await waitFor(() => {
      expect(screen.getByText('2026')).toBeInTheDocument()
    })

    // Encontra o botão de ano anterior dentro do dropdown
    const pickerContainer = document.querySelector('.absolute')
    const prevYearButton  = pickerContainer?.querySelectorAll('button')[0]

    await user.click(prevYearButton!)

    await waitFor(() => {
      expect(screen.getByText('2025')).toBeInTheDocument()
    })
  })

  it('deve bloquear meses futuros no picker', async () => {
    const onChange = vi.fn()
    const now      = new Date()
    renderPicker({
      month:    1,
      year:     now.getFullYear(),
      onChange,
      maxMonth: now.getMonth() + 1,
      maxYear:  now.getFullYear(),
    })
    const user = userEvent.setup()

    await user.click(screen.getByText(`Janeiro ${now.getFullYear()}`))

    await waitFor(() => {
      expect(screen.getByText('Jan')).toBeInTheDocument()
    })

    const allMonthLabels = [
      'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
      'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
    ]

    for (let i = now.getMonth() + 1; i < 12; i++) {
      const btn = screen.getByText(allMonthLabels[i])
      expect(btn).toBeDisabled()
    }
  })
})