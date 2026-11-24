import { useTranslation } from 'react-i18next'
import { useAccounts } from '../api/useAccounts'

interface AccountSelectProps {
  value:      string
  onChange:   (accountId: string) => void
  // Quando informado, adiciona uma primeira opção vazia traduzida:
  // 'all' → "Todas as contas" (filtros) · 'auto' → "Reconhecer automaticamente" (upload de OFX)
  emptyOption?: 'all' | 'auto'
  // Garante que uma conta arquivada já usada continue visível ao editar
  currentAccountId?: string
  // Esconde o seletor quando o utilizador só tem uma conta (evita ruído para quem não precisa)
  hideWhenSingle?: boolean
  style?:     React.CSSProperties
  id?:        string
  disabled?:  boolean
}

const baseStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  color:        'var(--ff-text-primary)',
  borderRadius: '8px',
  padding:      '8px 12px',
  fontSize:     '13px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

export default function AccountSelect({
  value, onChange, emptyOption, currentAccountId, hideWhenSingle = false, style, id, disabled,
}: AccountSelectProps) {
  const { t } = useTranslation('accounts')
  const { data: accounts = [] } = useAccounts(!!currentAccountId)

  const visible = accounts.filter(a => !a.isArchived || a.id === currentAccountId)

  if (hideWhenSingle && visible.length <= 1) return null

  return (
    <select
      id={id}
      disabled={disabled}
      aria-label={t('select.label')}
      value={value}
      onChange={e => onChange(e.target.value)}
      style={{ ...baseStyle, ...style }}
      onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
      onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
    >
      {emptyOption && (
        <option value="">
          {emptyOption === 'all' ? t('select.allAccounts') : t('import.auto')}
        </option>
      )}
      {visible.map(a => (
        <option key={a.id} value={a.id}>
          {a.name}{a.isArchived ? ` (${t('card.archived')})` : ''}
        </option>
      ))}
    </select>
  )
}
