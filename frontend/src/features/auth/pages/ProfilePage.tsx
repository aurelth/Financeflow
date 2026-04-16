import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Loader2, Lock, Settings, User } from 'lucide-react'
import { useState, useEffect } from 'react'
import { useUserProfile, useUpdateProfile, useChangePassword } from '../api/useAuth'

const preferencesSchema = z.object({
  currency: z.string().min(1).max(10),
  timezone: z.string().min(1).max(50),
  language: z.string().min(1), // Adicionado
})

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Senha atual é obrigatória'),
  newPassword: z.string()
    .min(8, 'Mínimo 8 caracteres')
    .regex(/[A-Z]/, 'Deve ter pelo menos uma maiúscula')
    .regex(/[0-9]/, 'Deve ter pelo menos um número')
    .regex(/[^a-zA-Z0-9]/, 'Deve ter pelo menos um símbolo'),
  confirmPassword: z.string(),
}).refine(d => d.newPassword === d.confirmPassword, {
  message: 'As senhas não coincidem',
  path: ['confirmPassword'],
})

type PreferencesForm = z.infer<typeof preferencesSchema>
type PasswordForm    = z.infer<typeof passwordSchema>

const CURRENCIES = [
  { value: 'BRL', label: 'Real Brasileiro (BRL)' },
  { value: 'USD', label: 'Dólar Americano (USD)' },
  { value: 'EUR', label: 'Euro (EUR)' },
  { value: 'GBP', label: 'Libra Esterlina (GBP)' },
]

const TIMEZONES = [
  { value: 'America/Sao_Paulo',  label: 'Brasília (GMT-3)'    },
  { value: 'America/Manaus',     label: 'Manaus (GMT-4)'      },
  { value: 'America/Belem',      label: 'Belém (GMT-3)'       },
  { value: 'America/Fortaleza',  label: 'Fortaleza (GMT-3)'   },
  { value: 'America/Recife',     label: 'Recife (GMT-3)'      },
  { value: 'America/New_York',   label: 'Nova York (GMT-5)'   },
  { value: 'America/Chicago',    label: 'Chicago (GMT-6)'     },
  { value: 'America/Los_Angeles',label: 'Los Angeles (GMT-8)' },
  { value: 'Europe/London',      label: 'Londres (GMT+0)'     },
  { value: 'Europe/Paris',       label: 'Paris (GMT+1)'       },
]

// Idiomas suportados
const LANGUAGES = [
  { value: 'pt-BR', label: 'Português (Brasil)' },
  { value: 'en-US', label: 'English (US)'        },
  { value: 'es-ES', label: 'Español'             },
  { value: 'fr-FR', label: 'Français'            },
]

const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '10px',
  padding:      '9px 14px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

const readonlyStyle: React.CSSProperties = {
  ...inputStyle,
  cursor: 'default',
  color:  'var(--ff-text-secondary)',
}

const labelStyle: React.CSSProperties = {
  display:       'block',
  fontSize:      '11px',
  fontWeight:    500,
  textTransform: 'uppercase',
  letterSpacing: '0.06em',
  color:         'var(--ff-text-muted)',
  marginBottom:  '4px',
}

function onHoverEnterBtnEmerald(e: React.MouseEvent<HTMLButtonElement>) {
  e.currentTarget.style.background = 'var(--ff-emerald-hover)'
}
function onHoverLeaveBtnEmerald(e: React.MouseEvent<HTMLButtonElement>) {
  e.currentTarget.style.background = 'var(--ff-emerald)'
}
function onHoverEnterTextPrimary(e: React.MouseEvent<HTMLButtonElement>) {
  e.currentTarget.style.color = 'var(--ff-text-primary)'
}
function onHoverLeaveTextMuted(e: React.MouseEvent<HTMLButtonElement>) {
  e.currentTarget.style.color = 'var(--ff-text-muted)'
}
function onFocusEmerald(e: React.FocusEvent<HTMLInputElement | HTMLSelectElement>) {
  e.currentTarget.style.borderColor = 'var(--ff-emerald)'
}
function onBlurBorder(e: React.FocusEvent<HTMLInputElement | HTMLSelectElement>) {
  e.currentTarget.style.borderColor = 'var(--ff-border)'
}

export default function ProfilePage() {
  const { data: profile, isLoading } = useUserProfile()
  const { mutate: updateProfile, isPending: isUpdating } = useUpdateProfile()
  const { mutate: changePassword, isPending: isChanging } = useChangePassword()

  const [showCurrent, setShowCurrent] = useState(false)
  const [showNew,     setShowNew]     = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const prefForm = useForm<PreferencesForm>({ resolver: zodResolver(preferencesSchema) })
  const passForm = useForm<PasswordForm>({ resolver: zodResolver(passwordSchema) })

  // Incluir language no reset
  useEffect(() => {
    if (profile) prefForm.reset({
      currency: profile.currency,
      timezone: profile.timezone,
      language: profile.language ?? 'pt-BR', // Adicionado
    })
  }, [profile])

  const genderLabel = (g: string) =>
    g === 'Male' ? 'Masculino' : g === 'Female' ? 'Feminino' : '—'

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
      </div>
    )
  }

  const prefErrors = prefForm.formState.errors
  const passErrors = passForm.formState.errors

  return (
    <div className="max-w-2xl mx-auto space-y-6">

      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>Perfil</h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          Gerencie suas informações e preferências
        </p>
      </div>

      {/* Dados pessoais */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-5">
          <User size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Dados pessoais
          </h2>
        </div>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label style={labelStyle}>Nome completo</label>
              <div style={readonlyStyle}>{profile?.name ?? '—'}</div>
            </div>
            <div className="space-y-1">
              <label style={labelStyle}>Email</label>
              <div style={readonlyStyle}>{profile?.email ?? '—'}</div>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label style={labelStyle}>CPF</label>
              <div style={readonlyStyle}>{profile?.cpf || '—'}</div>
            </div>
            <div className="space-y-1">
              <label style={labelStyle}>Gênero</label>
              <div style={readonlyStyle}>{genderLabel(profile?.gender ?? '')}</div>
            </div>
          </div>
        </div>

        <p className="text-xs mt-4" style={{ color: 'var(--ff-text-muted)' }}>
          Para alterar seu nome, envie um email para{' '}
          
            <a href="mailto:suporte@financeflow.com"
            className="transition-opacity hover:opacity-75"
            style={{ color: 'var(--ff-emerald)' }}
          >
            suporte@financeflow.com
          </a>
        </p>
      </div>

      {/* Preferências */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-5">
          <Settings size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>Preferências</h2>
        </div>

        <form onSubmit={prefForm.handleSubmit(d => updateProfile(d))} className="space-y-4">
          {/* grid-cols-2 → grid-cols-3 para acomodar idioma */}
          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-1">
              <label htmlFor="currency" style={labelStyle}>Moeda</label>
              <select
                id="currency"
                {...prefForm.register('currency')}
                style={inputStyle}
                onFocus={onFocusEmerald}
                onBlur={onBlurBorder}
              >
                {CURRENCIES.map(c => (
                  <option key={c.value} value={c.value} style={{ background: '#111' }}>
                    {c.label}
                  </option>
                ))}
              </select>
              {prefErrors.currency && (
                <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>
                  {prefErrors.currency?.message}
                </p>
              )}
            </div>

            <div className="space-y-1">
              <label htmlFor="timezone" style={labelStyle}>Fuso horário</label>
              <select
                id="timezone"
                {...prefForm.register('timezone')}
                style={inputStyle}
                onFocus={onFocusEmerald}
                onBlur={onBlurBorder}
              >
                {TIMEZONES.map(t => (
                  <option key={t.value} value={t.value} style={{ background: '#111' }}>
                    {t.label}
                  </option>
                ))}
              </select>
              {prefErrors.timezone && (
                <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>
                  {prefErrors.timezone?.message}
                </p>
              )}
            </div>

            {/* Seletor de idioma */}
            <div className="space-y-1">
              <label htmlFor="language" style={labelStyle}>Idioma</label>
              <select
                id="language"
                {...prefForm.register('language')}
                style={inputStyle}
                onFocus={onFocusEmerald}
                onBlur={onBlurBorder}
              >
                {LANGUAGES.map(l => (
                  <option key={l.value} value={l.value} style={{ background: '#111' }}>
                    {l.label}
                  </option>
                ))}
              </select>
              {prefErrors.language && (
                <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>
                  {prefErrors.language?.message}
                </p>
              )}
            </div>
          </div>

          <div className="flex justify-end">
            <button
              type="submit"
              disabled={isUpdating}
              className="px-6 h-10 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
              onMouseEnter={isUpdating ? undefined : onHoverEnterBtnEmerald}
              onMouseLeave={isUpdating ? undefined : onHoverLeaveBtnEmerald}
            >
              {isUpdating ? 'Salvando...' : 'Salvar preferências'}
            </button>
          </div>
        </form>
      </div>

      {/* Alterar senha */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2 mb-5">
          <Lock size={16} style={{ color: 'var(--ff-emerald)' }} />
          <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>Alterar senha</h2>
        </div>

        <form
          onSubmit={passForm.handleSubmit(d =>
            changePassword(d, { onSuccess: () => passForm.reset() })
          )}
          className="space-y-4"
        >
          <div className="space-y-1">
            <label htmlFor="currentPassword" style={labelStyle}>Senha atual</label>
            <div className="relative">
              <input
                id="currentPassword"
                type={showCurrent ? 'text' : 'password'}
                placeholder="••••••••"
                autoComplete="current-password"
                {...passForm.register('currentPassword')}
                style={{ ...inputStyle, paddingRight: '44px' }}
                onFocus={onFocusEmerald}
                onBlur={onBlurBorder}
              />
              <button
                type="button"
                onClick={() => setShowCurrent(p => !p)}
                className="absolute right-3 top-1/2 -translate-y-1/2 transition-colors"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={onHoverEnterTextPrimary}
                onMouseLeave={onHoverLeaveTextMuted}
              >
                {showCurrent ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
            {passErrors.currentPassword && (
              <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>
                {passErrors.currentPassword?.message}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label htmlFor="newPassword" style={labelStyle}>Nova senha</label>
              <div className="relative">
                <input
                  id="newPassword"
                  type={showNew ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  {...passForm.register('newPassword')}
                  style={{ ...inputStyle, paddingRight: '44px' }}
                  onFocus={onFocusEmerald}
                  onBlur={onBlurBorder}
                />
                <button
                  type="button"
                  onClick={() => setShowNew(p => !p)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 transition-colors"
                  style={{ color: 'var(--ff-text-muted)' }}
                  onMouseEnter={onHoverEnterTextPrimary}
                  onMouseLeave={onHoverLeaveTextMuted}
                >
                  {showNew ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
              </div>
              {passErrors.newPassword && (
                <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>
                  {passErrors.newPassword?.message}
                </p>
              )}
            </div>

            <div className="space-y-1">
              <label htmlFor="confirmPassword" style={labelStyle}>Confirmar senha</label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  type={showConfirm ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  {...passForm.register('confirmPassword')}
                  style={{ ...inputStyle, paddingRight: '44px' }}
                  onFocus={onFocusEmerald}
                  onBlur={onBlurBorder}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirm(p => !p)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 transition-colors"
                  style={{ color: 'var(--ff-text-muted)' }}
                  onMouseEnter={onHoverEnterTextPrimary}
                  onMouseLeave={onHoverLeaveTextMuted}
                >
                  {showConfirm ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
              </div>
              {passErrors.confirmPassword && (
                <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>
                  {passErrors.confirmPassword?.message}
                </p>
              )}
            </div>
          </div>

          <div className="flex justify-end">
            <button
              type="submit"
              disabled={isChanging}
              className="px-6 h-10 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
              onMouseEnter={isChanging ? undefined : onHoverEnterBtnEmerald}
              onMouseLeave={isChanging ? undefined : onHoverLeaveBtnEmerald}
            >
              {isChanging ? 'Alterando...' : 'Alterar senha'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}