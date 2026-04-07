import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useSearchParams } from 'react-router-dom'
import { KeyRound, ArrowLeft } from 'lucide-react'
import { useResetPassword } from '../api/useAuth'

const schema = z.object({
  newPassword:     z.string().min(8, 'Mínimo 8 caracteres.').regex(/[A-Z]/, 'Deve conter pelo menos uma letra maiúscula.').regex(/[0-9]/, 'Deve conter pelo menos um número.').regex(/[^a-zA-Z0-9]/, 'Deve conter pelo menos um símbolo.'),
  confirmPassword: z.string(),
}).refine(d => d.newPassword === d.confirmPassword, { message: 'As senhas não coincidem.', path: ['confirmPassword'] })

type FormData = z.infer<typeof schema>

const inputStyle: React.CSSProperties = {
  width: '100%', background: 'rgba(26,26,26,0.8)',
  border: '1px solid var(--ff-border)', borderRadius: '12px',
  padding: '11px 16px', color: 'var(--ff-text-primary)',
  fontSize: '14px', outline: 'none', transition: 'border-color 0.15s', height: '44px',
}

export default function ResetPasswordPage() {
  const [searchParams]                       = useSearchParams()
  const token                                = searchParams.get('token') ?? ''
  const { mutate: resetPassword, isPending } = useResetPassword()

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({ resolver: zodResolver(schema) })

  const onSubmit = (data: FormData) =>
    resetPassword({ token, newPassword: data.newPassword, confirmPassword: data.confirmPassword })

  if (!token) {
    return (
      <div className="w-full max-w-md mx-auto text-center">
        <p className="mb-4" style={{ color: 'var(--ff-expense)' }}>Link de redefinição inválido ou expirado.</p>
        <Link to="/login" className="text-sm transition-colors" style={{ color: 'var(--ff-emerald)' }}>
          Voltar para o login
        </Link>
      </div>
    )
  }

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="text-center mb-8">
        <div
          className="w-12 h-12 rounded-2xl flex items-center justify-center mx-auto mb-4"
          style={{ background: 'var(--ff-emerald-subtle)', border: '1px solid rgba(16,185,129,0.3)' }}
        >
          <KeyRound size={24} style={{ color: 'var(--ff-emerald)' }} />
        </div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          Redefinir senha
        </h1>
        <p className="mt-2 text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          Escolha uma nova senha para a sua conta.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-1.5">
          {/* Modificado: htmlFor + id */}
          <label htmlFor="newPassword" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Nova senha
          </label>
          <input
            id="newPassword"
            type="password"
            placeholder="••••••••"
            autoComplete="new-password"
            {...register('newPassword')}
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.newPassword && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.newPassword.message}</p>}
        </div>

        <div className="space-y-1.5">
          {/* Modificado: htmlFor + id */}
          <label htmlFor="confirmPassword" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Confirmar senha
          </label>
          <input
            id="confirmPassword"
            type="password"
            placeholder="••••••••"
            autoComplete="new-password"
            {...register('confirmPassword')}
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.confirmPassword && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.confirmPassword.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isPending}
          className="w-full h-11 rounded-xl font-semibold transition-colors disabled:opacity-50"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
        >
          {isPending ? 'Redefinindo...' : 'Redefinir senha'}
        </button>
      </form>

      <div className="mt-6 text-center">
        <Link
          to="/login"
          className="inline-flex items-center gap-2 text-sm transition-colors"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
        >
          <ArrowLeft size={14} />
          Voltar para o login
        </Link>
      </div>
    </div>
  )
}