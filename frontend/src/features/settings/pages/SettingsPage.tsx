import { useState, useEffect } from 'react'
import { Bell, LogOut, Trash2, Loader2, TriangleAlert, Globe } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import i18n, { SUPPORTED_LANGUAGES, LANGUAGE_LABELS } from '@/lib/i18n'
import { useAuthStore } from '@/store/authStore'
import { useUpdateProfile } from '@/features/auth/api/useAuth'
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
            background: '#fff',
            transform:  checked ? 'translateX(20px)' : 'translateX(0)',
            boxShadow:  '0 1px 3px rgba(0,0,0,0.3)',
          }}
        />
      </button>
    </div>
  )
}

export default function SettingsPage() {
  const { t }                              = useTranslation(['settings', 'common'])
  const { data: prefs, isLoading }         = useNotificationPreferences()
  const updatePrefs                        = useUpdateNotificationPreferences()
  const logoutAll                          = useLogoutAll()
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)

 // Idioma e moeda do perfil
  const user                               = useAuthStore(s => s.user)
  const { mutate: updateProfile, isPending: isSavingLang } = useUpdateProfile()
  const [selectedLanguage, setSelectedLanguage] = useState(
    user?.language ?? i18n.language ?? 'en-US'
  )

  const [budgetWarning,  setBudgetWarning]  = useState(true)
  const [budgetCritical, setBudgetCritical] = useState(true)
  const [dueTomorrow,    setDueTomorrow]    = useState(true)
  const [dueIn3Days,     setDueIn3Days]     = useState(true)

  useEffect(() => {
    if (prefs) {
      setBudgetWarning(prefs.budgetWarningEnabled)
      setBudgetCritical(prefs.budgetCriticalEnabled)
      setDueTomorrow(prefs.transactionDueTomorrowEnabled)
      setDueIn3Days(prefs.transactionDueIn3DaysEnabled)
    }
  }, [prefs])

 // Sincroniza idioma com o perfil ao carregar
  useEffect(() => {
    if (user?.language) setSelectedLanguage(user.language)
  }, [user?.language])

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

 // Aplica idioma imediatamente e persiste no perfil + localStorage
  function handleLanguageChange(lang: string) {
    setSelectedLanguage(lang)
    i18n.changeLanguage(lang)
    localStorage.setItem('ff_language', lang)
    if (user) {
      updateProfile({
        currency: user.currency,
        timezone: user.timezone,
        language: lang,
      })
    }
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
          {t('settings:page.title')}
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          {t('settings:page.subtitle')}
        </p>
      </div>

      {/* Seletor de idioma */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-2">
          <Globe size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {t('settings:sections.language')}
          </h2>
        </div>
        <p className="text-xs mb-5" style={{ color: 'var(--ff-text-muted)' }}>
          {t('settings:language.subtitle')}
        </p>

        <div className="grid grid-cols-2 gap-3">
          {SUPPORTED_LANGUAGES.map(lang => (
            <button
              key={lang}
              onClick={() => handleLanguageChange(lang)}
              disabled={isSavingLang}
              className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all disabled:opacity-50"
              style={selectedLanguage === lang
                ? { background: 'var(--ff-emerald-subtle)', border: '1px solid var(--ff-emerald)', color: 'var(--ff-emerald)' }
                : { background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }
              }
              onMouseEnter={e => {
                if (selectedLanguage !== lang)
                  e.currentTarget.style.borderColor = 'var(--ff-emerald)'
              }}
              onMouseLeave={e => {
                if (selectedLanguage !== lang)
                  e.currentTarget.style.borderColor = 'var(--ff-border)'
              }}
            >
              <span className="text-lg">
                {lang === 'pt-BR' ? '🇧🇷' : lang === 'en-US' ? '🇺🇸' : lang === 'es-ES' ? '🇪🇸' : '🇫🇷'}
              </span>
              {LANGUAGE_LABELS[lang]}
              {selectedLanguage === lang && (
                <span className="ml-auto text-xs" style={{ color: 'var(--ff-emerald)' }}>✓</span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Notificações */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-2">
          <Bell size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {t('settings:sections.notifications')}
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
            e.currentTarget.style.color      = 'var(--ff-text-primary)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.background = 'transparent'
            e.currentTarget.style.color      = 'var(--ff-text-secondary)'
          }}
        >
          <LogOut size={15} />
          {logoutAll.isPending ? t('common:actions.loading') : 'Encerrar todas as sessões'}
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

      {showDeleteDialog && (
        <DeleteAccountDialog onClose={() => setShowDeleteDialog(false)} />
      )}
    </div>
  )
}