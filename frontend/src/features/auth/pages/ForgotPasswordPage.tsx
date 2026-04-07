import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { Mail, ArrowLeft } from 'lucide-react'
import { useForgotPassword } from '../api/useAuth'

const schema = z.object({ email: z.string().email('Email inválido.') })
type FormData = z.infer<typeof schema>

const inputStyle: React.CSSProperties = {
  width: '100%', background: 'rgba(26,26,26,0.8)',
  border: '1px solid var(--ff-border)', borderRadius: '12px',
  padding: '11px 16px', color: 'var(--ff-text-primary)',
  fontSize: '14px', outline: 'none', transition: 'border-color 0.15s', height: '44px',
}

export default function ForgotPasswordPage() {
  const { mutate: forgotPassword, isPending } = useForgotPassword()
  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({ resolver: zodResolver(schema) })

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="text-center mb-8">
        <div
          className="w-12 h-12 rounded-2xl flex items-center justify-center mx-auto mb-4"
          style={{ background: 'var(--ff-emerald-subtle)', border: '1px solid rgba(16,185,129,0.3)' }}
        >
          <Mail size={24} style={{ color: 'var(--ff-emerald)' }} />
        </div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          Esqueceu a senha?
        </h1>
        <p className="mt-2 text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          Informe seu email e enviaremos um link para redefinir sua senha.
        </p>
      </div>

      <form onSubmit={handleSubmit(d => forgotPassword(d))} className="space-y-4">
        <div className="space-y-1.5">
          <label className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>Email</label>
          <input
            type="email"
            placeholder="seu@email.com"
            autoComplete="email"
            {...register('email')}
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.email && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.email.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isPending}
          className="w-full h-11 rounded-xl font-semibold transition-colors disabled:opacity-50"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
        >
          {isPending ? 'Enviando...' : 'Enviar link de redefinição'}
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