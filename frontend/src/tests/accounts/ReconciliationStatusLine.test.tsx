import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import ReconciliationStatusLine from '@/features/accounts/components/ReconciliationStatusLine'
import { makeReconciliation } from './fixtures'

vi.mock('react-i18next', async () => (await import('./i18nMock')).accountsI18nMock)

const renderLine = (overrides = {}) =>
  render(<ReconciliationStatusLine reconciliation={makeReconciliation(overrides)} currency="BRL" />)

describe('ReconciliationStatusLine', () => {
  it('não deve renderizar nada enquanto a conciliação não chegou', () => {
    const { container } = render(<ReconciliationStatusLine reconciliation={undefined} currency="BRL" />)

    expect(container).toBeEmptyDOMElement()
  })

  it('deve indicar conta conciliada com a data do extrato', () => {
    renderLine()

    expect(screen.getByTestId('reconciliation-matched'))
      .toHaveTextContent('Conciliada com o extrato de 28/02/2026')
    expect(screen.queryByTestId('reconciliation-divergent')).not.toBeInTheDocument()
  })

  it('deve alertar com o valor da diferença e os dois saldos', () => {
    renderLine({
      status: 'Divergent',
      statementBalance: 1500,
      calculatedBalance: 1400,
      difference: 100,
    })

    const alert = screen.getByTestId('reconciliation-divergent')
    expect(alert).toHaveTextContent(/Diferença de R\$\s?100,00 em relação ao extrato de 28\/02\/2026/)
    expect(alert).toHaveTextContent('Saldo do banco')
    expect(alert).toHaveTextContent(/R\$\s?1\.500,00/)
    expect(alert).toHaveTextContent('Saldo calculado')
    expect(alert).toHaveTextContent(/R\$\s?1\.400,00/)
  })

  it('deve orientar a conferir importação quando faltam entradas (diferença positiva)', () => {
    renderLine({ status: 'Divergent', statementBalance: 1500, calculatedBalance: 1400, difference: 100 })

    expect(screen.getByTestId('reconciliation-divergent')).toHaveTextContent('Faltam entradas no FinanceFlow')
  })

  it('deve orientar a conferir duplicados quando sobram entradas (diferença negativa)', () => {
    renderLine({ status: 'Divergent', statementBalance: 200, calculatedBalance: 250.5, difference: -50.5 })

    const alert = screen.getByTestId('reconciliation-divergent')
    // O valor é sempre positivo no texto; o sentido vem da orientação
    expect(alert).toHaveTextContent(/Diferença de R\$\s?50,50/)
    expect(alert).not.toHaveTextContent('-')
    expect(alert).toHaveTextContent('Há entradas a mais no FinanceFlow')
  })

  it('deve convidar a importar um OFX quando não há extrato com saldo', () => {
    renderLine({ status: 'NoStatement', statementBalance: null, calculatedBalance: null, difference: null })

    expect(screen.getByTestId('reconciliation-none'))
      .toHaveTextContent('Sem extrato com saldo. Importe um OFX para conciliar esta conta.')
  })

  it('deve explicar quando a comparação é impossível', () => {
    renderLine({ status: 'Unavailable', calculatedBalance: null, difference: null })

    expect(screen.getByTestId('reconciliation-none'))
      .toHaveTextContent('o saldo inicial da conta é posterior à data do extrato')
  })
})
