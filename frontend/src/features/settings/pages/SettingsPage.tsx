import { useState, useEffect } from 'react'
import { Bell, LogOut, Trash2, Loader2, TriangleAlert } from 'lucide-react'
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
  useLogoutAll,
} from '../api/useSettings'
import DeleteAccountDialog from '../components/DeleteAccountDialog'

interface ToggleProps {
  label:       string
  description: string
  checked:     boolean
  onChange:    (val: boolean) => void
}

function Toggle({ label, description, checked, onChange }: ToggleProps) {
  return (
    <div className="flex items-center justify-between py-3">
      <div className="flex-1 min-w-0 pr-4">
        <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
          {label}
        </p>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          {description}
        </p>
      </div>
      <button
        type="button"
        onClick={() => onChange(!checked)}
        className="relative flex-shrink-0 w-11 h-6 rounded-full transition-colors duration-200 focus:outline-none"
        style={{ background: checked ? 'var(--ff-emerald)' : 'var(--ff-bg-elevated)' }}
      >
        <span
          className="absolute top-0.5 left-0.5 w-5 h-5 rounded-full transition-transform duration-200"
          style={{
            background:  '#fff',
            transform:   checked ? 'translateX(20px)' : 'translateX(0)',
            boxShadow:   '0 1px 3px rgba(0,0,0,0.3)',
          }}
        />
      </button>
    </div>
  )
}

export default function SettingsPage() {
  const { data: prefs, isLoading } = useNotificationPreferences()
  const updatePrefs                = useUpdateNotificationPreferences()
  const logoutAll                  = useLogoutAll()
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)

  // Estado local dos toggles — inicializado com os dados da API
  const [budgetWarning,    setBudgetWarning]    = useState(true)
  const [budgetCritical,   setBudgetCritical]   = useState(true)
  const [dueTomorrow,      setDueTomorrow]      = useState(true)
  const [dueIn3Days,       setDueIn3Days]       = useState(true)

  useEffect(() => {
    if (prefs) {
      setBudgetWarning(prefs.budgetWarningEnabled)
      setBudgetCritical(prefs.budgetCriticalEnabled)
      setDueTomorrow(prefs.transactionDueTomorrowEnabled)
      setDueIn3Days(prefs.transactionDueIn3DaysEnabled)
    }
  }, [prefs])

  function handleToggle(
    setter: (val: boolean) => void,
    field:  keyof typeof currentPrefs,
    value:  boolean
  ) {
    setter(value)
    updatePrefs.mutate({
      budgetWarningEnabled:          field === 'budgetWarningEnabled'          ? value : budgetWarning,
      budgetCriticalEnabled:         field === 'budgetCriticalEnabled'         ? value : budgetCritical,
      transactionDueTomorrowEnabled: field === 'transactionDueTomorrowEnabled' ? value : dueTomorrow,
      transactionDueIn3DaysEnabled:  field === 'transactionDueIn3DaysEnabled'  ? value : dueIn3Days,
    })
  }

  const currentPrefs = {
    budgetWarningEnabled:          budgetWarning,
    budgetCriticalEnabled:         budgetCritical,
    transactionDueTomorrowEnabled: dueTomorrow,
    transactionDueIn3DaysEnabled:  dueIn3Days,
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">

      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          Configurações
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          Gerencie as preferências do sistema e da sua conta
        </p>
      </div>

      {/* Notificações */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-2">
          <Bell size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Notificações
          </h2>
        </div>
        <p className="text-xs mb-4" style={{ color: 'var(--ff-text-muted)' }}>
          Escolha quais alertas deseja receber
        </p>

        <div style={{ borderTop: '1px solid var(--ff-border)' }}>
          <Toggle
            label="Aviso de orçamento (80%)"
            description="Receba um alerta quando atingir 80% do limite de um orçamento"
            checked={budgetWarning}
            onChange={val => handleToggle(setBudgetWarning, 'budgetWarningEnabled', val)}
          />
          <div style={{ borderTop: '1px solid var(--ff-border-subtle)' }}>
            <Toggle
              label="Limite de orçamento atingido (100%)"
              description="Receba um alerta quando ultrapassar o limite de um orçamento"
              checked={budgetCritical}
              onChange={val => handleToggle(setBudgetCritical, 'budgetCriticalEnabled', val)}
            />
          </div>
          <div style={{ borderTop: '1px solid var(--ff-border-subtle)' }}>
            <Toggle
              label="Transação vence amanhã"
              description="Receba um alerta quando uma despesa vencer no dia seguinte"
              checked={dueTomorrow}
              onChange={val => handleToggle(setDueTomorrow, 'transactionDueTomorrowEnabled', val)}
            />
          </div>
          <div style={{ borderTop: '1px solid var(--ff-border-subtle)' }}>
            <Toggle
              label="Transação vence em 3 dias"
              description="Receba um alerta quando uma despesa vencer em 3 dias"
              checked={dueIn3Days}
              onChange={val => handleToggle(setDueIn3Days, 'transactionDueIn3DaysEnabled', val)}
            />
          </div>
        </div>
      </div>

      {/* Sessão */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-2">
          <LogOut size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Sessão
          </h2>
        </div>
        <p className="text-xs mb-5" style={{ color: 'var(--ff-text-muted)' }}>
          Encerre todas as sessões activas em outros dispositivos
        </p>

        <button
          onClick={() => logoutAll.mutate()}
          disabled={logoutAll.isPending}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
          style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
          onMouseEnter={e => {
            e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            e.currentTarget.style.color = 'var(--ff-text-primary)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.background = 'transparent'
            e.currentTarget.style.color = 'var(--ff-text-secondary)'
          }}
        >
          <LogOut size={15} />
          {logoutAll.isPending ? 'Encerrando...' : 'Encerrar todas as sessões'}
        </button>
      </div>

      {/* Zona de perigo */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'rgba(244,63,94,0.04)', border: '1px solid rgba(244,63,94,0.2)' }}
      >
        <div className="flex items-center gap-2 mb-2">
          <TriangleAlert size={16} style={{ color: 'var(--ff-expense)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-expense)' }}>
            Zona de perigo
          </h2>
        </div>
        <p className="text-xs mb-5" style={{ color: 'var(--ff-text-muted)' }}>
          Ações irreversíveis — proceda com cuidado
        </p>

        <button
          onClick={() => setShowDeleteDialog(true)}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'rgba(244,63,94,0.1)', color: 'var(--ff-expense)', border: '1px solid rgba(244,63,94,0.3)' }}
          onMouseEnter={e => (e.currentTarget.style.background = 'rgba(244,63,94,0.18)')}
          onMouseLeave={e => (e.currentTarget.style.background = 'rgba(244,63,94,0.1)')}
        >
          <Trash2 size={15} />
          Excluir conta
        </button>
      </div>

      {/* Modal de confirmação */}
      {showDeleteDialog && (
        <DeleteAccountDialog onClose={() => setShowDeleteDialog(false)} />
      )}
    </div>
  )
}