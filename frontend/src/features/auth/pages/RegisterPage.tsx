import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { Eye, EyeOff, Loader2, Sparkles } from 'lucide-react'
import { useState } from 'react'
import { useRegister } from '../api/useAuth'

const isValidCpf = (cpf: string): boolean => {
  const digits = cpf.replace(/\D/g, '')
  if (digits.length !== 11) return false
  if (/^(\d)\1{10}$/.test(digits)) return false
  let sum = 0
  for (let i = 0; i < 9; i++) sum += parseInt(digits[i]) * (10 - i)
  let remainder = sum % 11
  const first = remainder < 2 ? 0 : 11 - remainder
  if (first !== parseInt(digits[9])) return false
  sum = 0
  for (let i = 0; i < 10; i++) sum += parseInt(digits[i]) * (11 - i)
  remainder = sum % 11
  const second = remainder < 2 ? 0 : 11 - remainder
  return second === parseInt(digits[10])
}

const maskCpf = (value: string): string => {
  const digits = value.replace(/\D/g, '').slice(0, 11)
  if (digits.length <= 3) return digits
  if (digits.length <= 6) return `${digits.slice(0, 3)}.${digits.slice(3)}`
  if (digits.length <= 9) return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`
}

const schema = z.object({
  name:            z.string().min(2, 'Nome deve ter pelo menos 2 caracteres').max(100),
  cpf:             z.string().regex(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/, 'CPF deve estar no formato 000.000.000-00').refine(isValidCpf, 'CPF inválido'),
  gender:          z.enum(['Male', 'Female']),
  email:           z.string().email('Email inválido'),
  password:        z.string().min(8, 'Mínimo 8 caracteres').regex(/[A-Z]/, 'Deve ter pelo menos uma maiúscula').regex(/[0-9]/, 'Deve ter pelo menos um número').regex(/[^a-zA-Z0-9]/, 'Deve ter pelo menos um símbolo'),
  confirmPassword: z.string(),
}).refine(d => d.password === d.confirmPassword, { message: 'As senhas não coincidem', path: ['confirmPassword'] })

type FormData = z.infer<typeof schema>

const inputStyle: React.CSSProperties = {
  width: '100%', background: 'rgba(26,26,26,0.8)',
  border: '1px solid var(--ff-border)', borderRadius: '12px',
  padding: '11px 16px', color: 'var(--ff-text-primary)',
  fontSize: '14px', outline: 'none', transition: 'border-color 0.15s', height: '44px',
}

const selectStyle: React.CSSProperties = { ...inputStyle, cursor: 'pointer' }

export default function RegisterPage() {
  const [showPassword, setShowPassword]  = useState(false)
  const [showConfirm,  setShowConfirm]   = useState(false)
  const { mutate: register_, isPending } = useRegister()

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const onSubmit = ({ name, cpf, gender, email, password }: FormData) =>
    register_({ name, cpf, gender, email, password, currency: 'BRL', timezone: 'America/Sao_Paulo' })

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
          <Sparkles style={{ color: 'var(--ff-emerald)' }} size={26} />
        </div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          Criar sua conta
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          Comece a controlar suas finanças hoje
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

        {/* Nome — Modificado: htmlFor + id */}
        <div className="space-y-1.5">
          <label htmlFor="name" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Nome completo
          </label>
          <input
            id="name"
            placeholder="Seu nome completo"
            {...register('name')}
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.name && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.name.message}</p>}
        </div>

        <div className="grid grid-cols-2 gap-3">
          {/* CPF — Modificado: htmlFor + id */}
          <div className="space-y-1.5">
            <label htmlFor="cpf" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
              CPF
            </label>
            <input
              id="cpf"
              placeholder="000.000.000-00"
              value={watch('cpf') ?? ''}
              onChange={e => setValue('cpf', maskCpf(e.target.value), { shouldValidate: true })}
              style={inputStyle}
              onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
              onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
            />
            {errors.cpf && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.cpf.message}</p>}
          </div>

          {/* Gênero — Modificado: htmlFor + id */}
          <div className="space-y-1.5">
            <label htmlFor="gender" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
              Gênero
            </label>
            <select
              id="gender"
              {...register('gender')}
              style={selectStyle}
              onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
              onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
            >
              <option value="" style={{ background: '#111' }}>Selecione</option>
              <option value="Male" style={{ background: '#111' }}>Masculino</option>
              <option value="Female" style={{ background: '#111' }}>Feminino</option>
            </select>
            {errors.gender && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.gender.message}</p>}
          </div>
        </div>

        {/* Email — Modificado: htmlFor + id */}
        <div className="space-y-1.5">
          <label htmlFor="email" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Email
          </label>
          <input
            id="email"
            type="email"
            placeholder="seu@email.com"
            {...register('email')}
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.email && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.email.message}</p>}
        </div>

        {/* Senha — Modificado: htmlFor + id */}
        <div className="space-y-1.5">
          <label htmlFor="password" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Senha
          </label>
          <div className="relative">
            <input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              {...register('password')}
              style={{ ...inputStyle, paddingRight: '44px' }}
              onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
              onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
            />
            <button type="button" onClick={() => setShowPassword(p => !p)}
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

        {/* Confirmar senha — Modificado: htmlFor + id */}
        <div className="space-y-1.5">
          <label htmlFor="confirmPassword" className="block text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Confirmar senha
          </label>
          <div className="relative">
            <input
              id="confirmPassword"
              type={showConfirm ? 'text' : 'password'}
              placeholder="••••••••"
              {...register('confirmPassword')}
              style={{ ...inputStyle, paddingRight: '44px' }}
              onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
              onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
            />
            <button type="button" onClick={() => setShowConfirm(p => !p)}
              className="absolute right-3 top-1/2 -translate-y-1/2 transition-colors"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
            >
              {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.confirmPassword && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.confirmPassword.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isPending}
          className="w-full h-11 rounded-xl font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-2 mt-2"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
        >
          {isPending ? <><Loader2 size={16} className="animate-spin" />Criando conta...</> : 'Criar conta'}
        </button>
      </form>

      <p className="text-center text-sm mt-6" style={{ color: 'var(--ff-text-muted)' }}>
        Já tem uma conta?{' '}
        <Link to="/login" className="font-medium transition-colors" style={{ color: 'var(--ff-emerald)' }}>
          Entrar
        </Link>
      </p>
    </div>
  )
}