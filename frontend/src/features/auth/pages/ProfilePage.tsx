import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Loader2, Lock, Settings, User } from 'lucide-react'
import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useUserProfile, useUpdateProfile, useChangePassword } from '../api/useAuth'

// Schema de preferências
const preferencesSchema = z.object({
  currency: z.string().min(1, 'Moeda é obrigatória').max(10, 'Máximo 10 caracteres'),
  timezone: z.string().min(1, 'Fuso horário é obrigatório').max(50, 'Máximo 50 caracteres'),
})

// Schema de alteração de senha
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
  { value: 'America/Sao_Paulo',  label: 'Brasília (GMT-3)' },
  { value: 'America/Manaus',     label: 'Manaus (GMT-4)' },
  { value: 'America/Belem',      label: 'Belém (GMT-3)' },
  { value: 'America/Fortaleza',  label: 'Fortaleza (GMT-3)' },
  { value: 'America/Recife',     label: 'Recife (GMT-3)' },
  { value: 'America/New_York',   label: 'Nova York (GMT-5)' },
  { value: 'America/Chicago',    label: 'Chicago (GMT-6)' },
  { value: 'America/Los_Angeles', label: 'Los Angeles (GMT-8)' },
  { value: 'Europe/London',      label: 'Londres (GMT+0)' },
  { value: 'Europe/Paris',       label: 'Paris (GMT+1)' },
]

export default function ProfilePage() {
  const { data: profile, isLoading }      = useUserProfile()
  const { mutate: updateProfile, isPending: isUpdating } = useUpdateProfile()
  const { mutate: changePassword, isPending: isChanging } = useChangePassword()

  const [showCurrent, setShowCurrent]   = useState(false)
  const [showNew, setShowNew]           = useState(false)
  const [showConfirm, setShowConfirm]   = useState(false)

  const prefForm = useForm<PreferencesForm>({
    resolver: zodResolver(preferencesSchema),
  })

  const passForm = useForm<PasswordForm>({
    resolver: zodResolver(passwordSchema),
  })

  useEffect(() => {
    if (profile) {
      prefForm.reset({
        currency: profile.currency,
        timezone: profile.timezone,
      })
    }
  }, [profile])

  const onSavePreferences = (data: PreferencesForm) =>
    updateProfile(data)

  const onChangePassword = (data: PasswordForm) => {
    changePassword(
      {
        currentPassword: data.currentPassword,
        newPassword:     data.newPassword,
        confirmPassword: data.confirmPassword,
      },
      {
        onSuccess: () => passForm.reset(),
      }
    )
  }

  const genderLabel = (gender: string) =>
    gender === 'Male' ? 'Masculino' : gender === 'Female' ? 'Feminino' : '—'

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 size={24} className="animate-spin text-indigo-400" />
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">

      {/* Título */}
      <div>
        <h1 className="text-2xl font-bold text-white">Perfil</h1>
        <p className="text-slate-400 text-sm mt-1">Gerencie suas informações e preferências</p>
      </div>

      {/* Dados de identidade — somente leitura */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <div className="flex items-center gap-2 mb-5">
          <User size={16} className="text-indigo-400" />
          <h2 className="text-white font-semibold">Dados pessoais</h2>
        </div>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs uppercase tracking-wide">Nome completo</Label>
              <p className="text-white text-sm bg-slate-800/50 border border-slate-700 rounded-md px-3 py-2.5">
                {profile?.name ?? '—'}
              </p>
            </div>
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs uppercase tracking-wide">Email</Label>
              <p className="text-white text-sm bg-slate-800/50 border border-slate-700 rounded-md px-3 py-2.5">
                {profile?.email ?? '—'}
              </p>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs uppercase tracking-wide">CPF</Label>
              <p className="text-white text-sm bg-slate-800/50 border border-slate-700 rounded-md px-3 py-2.5">
                {profile?.cpf || '—'}
              </p>
            </div>
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs uppercase tracking-wide">Gênero</Label>
              <p className="text-white text-sm bg-slate-800/50 border border-slate-700 rounded-md px-3 py-2.5">
                {genderLabel(profile?.gender ?? '')}
              </p>
            </div>
          </div>
        </div>

        <p className="text-slate-500 text-xs mt-4">
          Para alterar seu nome, envie um email para{' '}
          <a href="mailto:suporte@financeflow.com" className="text-indigo-400 hover:text-indigo-300 transition-colors">
            suporte@financeflow.com
          </a>
        </p>
      </div>

      {/* Preferências — editável */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <div className="flex items-center gap-2 mb-5">
          <Settings size={16} className="text-indigo-400" />
          <h2 className="text-white font-semibold">Preferências</h2>
        </div>

        <form onSubmit={prefForm.handleSubmit(onSavePreferences)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="currency" className="text-slate-300 text-sm">Moeda</Label>
              <select
                id="currency"
                {...prefForm.register('currency')}
                className="w-full h-10 px-3 rounded-md bg-slate-800/50 border border-slate-700 text-white focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500/20 text-sm"
              >
                {CURRENCIES.map(c => (
                  <option key={c.value} value={c.value} className="bg-slate-800">{c.label}</option>
                ))}
              </select>
              {prefForm.formState.errors.currency && (
                <p className="text-red-400 text-xs">{prefForm.formState.errors.currency.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="timezone" className="text-slate-300 text-sm">Fuso horário</Label>
              <select
                id="timezone"
                {...prefForm.register('timezone')}
                className="w-full h-10 px-3 rounded-md bg-slate-800/50 border border-slate-700 text-white focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500/20 text-sm"
              >
                {TIMEZONES.map(t => (
                  <option key={t.value} value={t.value} className="bg-slate-800">{t.label}</option>
                ))}
              </select>
              {prefForm.formState.errors.timezone && (
                <p className="text-red-400 text-xs">{prefForm.formState.errors.timezone.message}</p>
              )}
            </div>
          </div>

          <div className="flex justify-end">
            <Button
              type="submit"
              disabled={isUpdating}
              className="bg-indigo-600 hover:bg-indigo-500 text-white font-medium px-6"
            >
              {isUpdating
                ? <><Loader2 size={14} className="animate-spin mr-2" />Salvando...</>
                : 'Salvar preferências'
              }
            </Button>
          </div>
        </form>
      </div>

      {/* Alterar senha */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <div className="flex items-center gap-2 mb-5">
          <Lock size={16} className="text-indigo-400" />
          <h2 className="text-white font-semibold">Alterar senha</h2>
        </div>

        <form onSubmit={passForm.handleSubmit(onChangePassword)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="currentPassword" className="text-slate-300 text-sm">Senha atual</Label>
            <div className="relative">
              <Input
                id="currentPassword"
                type={showCurrent ? 'text' : 'password'}
                placeholder="••••••••"
                autoComplete="current-password"
                {...passForm.register('currentPassword')}
                className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500 h-10 pr-11"
              />
              <button type="button" onClick={() => setShowCurrent(p => !p)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
                {showCurrent ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
            {passForm.formState.errors.currentPassword && (
              <p className="text-red-400 text-xs">{passForm.formState.errors.currentPassword.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="newPassword" className="text-slate-300 text-sm">Nova senha</Label>
              <div className="relative">
                <Input
                  id="newPassword"
                  type={showNew ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  {...passForm.register('newPassword')}
                  className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500 h-10 pr-11"
                />
                <button type="button" onClick={() => setShowNew(p => !p)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
                  {showNew ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
              </div>
              {passForm.formState.errors.newPassword && (
                <p className="text-red-400 text-xs">{passForm.formState.errors.newPassword.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword" className="text-slate-300 text-sm">Confirmar senha</Label>
              <div className="relative">
                <Input
                  id="confirmPassword"
                  type={showConfirm ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  {...passForm.register('confirmPassword')}
                  className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500 h-10 pr-11"
                />
                <button type="button" onClick={() => setShowConfirm(p => !p)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
                  {showConfirm ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
              </div>
              {passForm.formState.errors.confirmPassword && (
                <p className="text-red-400 text-xs">{passForm.formState.errors.confirmPassword.message}</p>
              )}
            </div>
          </div>

          <div className="flex justify-end">
            <Button
              type="submit"
              disabled={isChanging}
              className="bg-indigo-600 hover:bg-indigo-500 text-white font-medium px-6"
            >
              {isChanging
                ? <><Loader2 size={14} className="animate-spin mr-2" />Alterando...</>
                : 'Alterar senha'
              }
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}