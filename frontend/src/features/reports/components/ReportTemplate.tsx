import { forwardRef } from 'react'
import type { Transaction } from '@/features/transactions/types/transaction.types'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus } from '@/features/transactions/types/transaction.types'
import type { DashboardSummary } from '@/features/dashboard/types/dashboard.types'

interface ReportTemplateProps {
  month:        number
  year:         number
  summary:      DashboardSummary | undefined
  transactions: Transaction[]
}

const MONTHS = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

const statusLabel: Record<TransactionStatus, string> = {
  [TransactionStatus.Paid]:      'Pago',
  [TransactionStatus.Pending]:   'Pendente',
  [TransactionStatus.Scheduled]: 'Agendado',
}

function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('pt-BR')
}

const ReportTemplate = forwardRef<HTMLDivElement, ReportTemplateProps>(
  ({ month, year, summary, transactions }, ref) => {
    const periodLabel = `${MONTHS[month - 1]} de ${year}`

    const byCategory = transactions
      .filter(t => t.type === TransactionType.Expense)
      .reduce<Record<string, { name: string; icon: string; total: number }>>((acc, t) => {
        if (!acc[t.categoryId]) {
          acc[t.categoryId] = { name: t.categoryName, icon: t.categoryIcon, total: 0 }
        }
        acc[t.categoryId].total += t.amount
        return acc
      }, {})

    const categoryTotals = Object.values(byCategory)
      .sort((a, b) => b.total - a.total)

    return (
      <div
        ref={ref}
        style={{
          fontFamily:      'Arial, sans-serif',
          fontSize:        '12px',
          color:           '#1e293b',
          backgroundColor: '#ffffff',
          padding:         '32px',
          maxWidth:        '800px',
          margin:          '0 auto',
        }}
      >
        {/* Cabeçalho */}
        <div style={{ borderBottom: '2px solid #6366f1', paddingBottom: '16px', marginBottom: '24px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontSize: '22px', fontWeight: 'bold', color: '#6366f1' }}>
                FinanceFlow
              </div>
              <div style={{ fontSize: '11px', color: '#64748b' }}>Gestão Financeira Pessoal</div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: '16px', fontWeight: 'bold' }}>Relatório Financeiro</div>
              <div style={{ color: '#64748b' }}>{periodLabel}</div>
              <div style={{ color: '#64748b', fontSize: '10px' }}>
                Gerado em {new Date().toLocaleDateString('pt-BR')}
              </div>
            </div>
          </div>
        </div>

        {/* Sumário */}
        {summary && (
          <div style={{ marginBottom: '24px' }}>
            <div style={{ fontSize: '14px', fontWeight: 'bold', marginBottom: '12px', color: '#374151' }}>
              Resumo do Período
            </div>
            <div style={{ display: 'flex', gap: '12px' }}>
              {[
                { label: 'Receitas',  value: summary.totalIncome,   color: '#10b981' },
                { label: 'Despesas',  value: summary.totalExpenses, color: '#ef4444' },
                { label: 'Saldo',     value: summary.balance,       color: '#6366f1' },
              ].map(item => (
                <div
                  key={item.label}
                  style={{
                    flex:            1,
                    border:          `1px solid ${item.color}40`,
                    borderRadius:    '8px',
                    padding:         '12px',
                    backgroundColor: `${item.color}10`,
                  }}
                >
                  <div style={{ fontSize: '10px', color: '#64748b', marginBottom: '4px' }}>
                    {item.label}
                  </div>
                  <div style={{ fontSize: '16px', fontWeight: 'bold', color: item.color }}>
                    {formatCurrency(item.value)}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Top categorias */}
        {categoryTotals.length > 0 && (
          <div style={{ marginBottom: '24px' }}>
            <div style={{ fontSize: '14px', fontWeight: 'bold', marginBottom: '12px', color: '#374151' }}>
              Despesas por Categoria
            </div>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ backgroundColor: '#f8fafc' }}>
                  <th style={{ textAlign: 'left', padding: '8px', borderBottom: '1px solid #e2e8f0', color: '#64748b', fontWeight: 'normal' }}>Categoria</th>
                  <th style={{ textAlign: 'right', padding: '8px', borderBottom: '1px solid #e2e8f0', color: '#64748b', fontWeight: 'normal' }}>Total</th>
                  <th style={{ textAlign: 'right', padding: '8px', borderBottom: '1px solid #e2e8f0', color: '#64748b', fontWeight: 'normal' }}>%</th>
                </tr>
              </thead>
              <tbody>
                {categoryTotals.map((cat, i) => {
                  const pct = summary
                    ? ((cat.total / summary.totalExpenses) * 100).toFixed(1)
                    : '—'
                  return (
                    <tr key={i} style={{ borderBottom: '1px solid #f1f5f9' }}>
                      <td style={{ padding: '8px' }}>{cat.icon} {cat.name}</td>
                      <td style={{ padding: '8px', textAlign: 'right', color: '#ef4444', fontWeight: 'bold' }}>
                        {formatCurrency(cat.total)}
                      </td>
                      <td style={{ padding: '8px', textAlign: 'right', color: '#64748b' }}>{pct}%</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Transações */}
        <div>
          <div style={{ fontSize: '14px', fontWeight: 'bold', marginBottom: '12px', color: '#374151' }}>
            Transações do Período ({transactions.length})
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ backgroundColor: '#f8fafc' }}>
                {['Data', 'Descrição', 'Categoria', 'Status', 'Valor'].map(h => (
                  <th
                    key={h}
                    style={{
                      textAlign:    h === 'Valor' ? 'right' : 'left',
                      padding:      '8px',
                      borderBottom: '1px solid #e2e8f0',
                      color:        '#64748b',
                      fontWeight:   'normal',
                      fontSize:     '11px',
                    }}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {transactions.map(tx => (
                <tr key={tx.id} style={{ borderBottom: '1px solid #f1f5f9' }}>
                  <td style={{ padding: '7px 8px', whiteSpace: 'nowrap' }}>{formatDate(tx.date)}</td>
                  <td style={{ padding: '7px 8px', maxWidth: '200px' }}>{tx.description || '—'}</td>
                  <td style={{ padding: '7px 8px' }}>{tx.categoryIcon} {tx.categoryName}</td>
                  <td style={{ padding: '7px 8px' }}>{statusLabel[tx.status]}</td>
                  <td style={{
                    padding:    '7px 8px',
                    textAlign:  'right',
                    fontWeight: 'bold',
                    color:      tx.type === TransactionType.Income ? '#10b981' : '#ef4444',
                    whiteSpace: 'nowrap',
                  }}>
                    {tx.type === TransactionType.Income ? '+' : '-'}
                    {formatCurrency(tx.amount)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Rodapé */}
        <div style={{
          marginTop:   '32px',
          paddingTop:  '12px',
          borderTop:   '1px solid #e2e8f0',
          textAlign:   'center',
          color:       '#94a3b8',
          fontSize:    '10px',
        }}>
          FinanceFlow — Relatório gerado automaticamente em {new Date().toLocaleString('pt-BR')}
        </div>
      </div>
    )
  }
)

ReportTemplate.displayName = 'ReportTemplate'

export default ReportTemplate