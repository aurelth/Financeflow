import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { Eye, EyeOff, Loader2, TrendingUp } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next' // Adicionado
import { useLogin } from '../api/useAuth'

// Schema com mensagens i18n resolvidas fora do componente
const schema = z.object({
  email:    z.string().email('Email inválido'),
  password: z.string().min(1, 'Senha obrigatória'),
})
type FormData = z.infer<typeof schema>

const inputStyle: React.CSSProperties = {
  width: '100%', background: 'rgba(26,26,26,0.8)',
  border: '1px solid var(--ff-border)', borderRadius: '12px',
  padding: '11px 16px', color: 'var(--ff-text-primary)',
  fontSize: '14px', outline: 'none', transition: 'border-color 0.15s', height: '44px',
}

export default function LoginPage() {
  const { t }                           = useTranslation('auth') // Adicionado
  const [showPassword, setShowPassword] = useState(false)
  const { mutate: login, isPending }    = useLogin()

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  return (
    <div
      className="rounded-2xl p-8 shadow-2xl"
      style={{ background: 'rgba(17,17,17,0.9)', border: '1px solid var(--ff-border)', backdropFilter: 'blur(20px)' }}
    >
      <div className="text-center mb-8">
        <div
          className="inline-flex items-center justify-center w-14 h-14 rounded-2xl mb-4"
          style={{ background: 'var(--ff-emerald-subtle)', border: '1px solid rgba(16,185,129,0.3)' }}
        >
          <TrendingUp style={{ color: 'var(--ff-emerald)' }} size={26} />
        </div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          {t('login.title')}
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          {t('login.subtitle')}
        </p>
      </div>

      <form onSubmit={handleSubmit(d => login(d))} className="space-y-5">
        <div className="space-y-1.5">
          <label htmlFor="email" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            {t('login.email')}
          </label>
          <input
            id="email"
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

        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <label htmlFor="password" className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
              {t('login.password')}
            </label>
            <Link
              to="/forgot-password"
              className="text-xs transition-colors"
              style={{ color: 'var(--ff-emerald)' }}
            >
              {t('login.forgotPassword')}
            </Link>
          </div>
          <div className="relative">
            <input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              autoComplete="current-password"
              {...register('password')}
              style={{ ...inputStyle, paddingRight: '44px' }}
              onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
              onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
            />
            <button
              type="button"
              onClick={() => setShowPassword(p => !p)}
              className="absolute right-3 top-1/2 -translate-y-1/2 transition-colors"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.password && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.password.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isPending}
          className="w-full h-11 rounded-xl font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
        >
          {isPending
            ? <><Loader2 size={16} className="animate-spin" />{t('common:actions.loading')}</>
            : t('login.submit')
          }
        </button>
      </form>

      <p className="text-center text-sm mt-6" style={{ color: 'var(--ff-text-muted)' }}>
        {t('login.noAccount')}{' '}
        <Link to="/register" className="font-medium transition-colors" style={{ color: 'var(--ff-emerald)' }}>
          {t('login.register')}
        </Link>
      </p>
    </div>
  )
}