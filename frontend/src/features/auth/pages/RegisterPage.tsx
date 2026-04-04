import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { Eye, EyeOff, Loader2, Sparkles } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
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
  name: z.string()
    .min(2, 'Nome deve ter pelo menos 2 caracteres')
    .max(100, 'Nome muito longo'),
  cpf: z.string()
    .regex(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/, 'CPF deve estar no formato 000.000.000-00')
    .refine(isValidCpf, 'CPF inválido'),
  gender: z.enum(['Male', 'Female']),
  email: z.string().email('Email inválido'),
  password: z.string()
    .min(8, 'Mínimo 8 caracteres')
    .regex(/[A-Z]/, 'Deve ter pelo menos uma maiúscula')
    .regex(/[0-9]/, 'Deve ter pelo menos um número')
    .regex(/[^a-zA-Z0-9]/, 'Deve ter pelo menos um símbolo'),
  confirmPassword: z.string(),
}).refine(d => d.password === d.confirmPassword, {
  message: 'As senhas não coincidem',
  path: ['confirmPassword'],
})

type FormData = z.infer<typeof schema>

export default function RegisterPage() {
  const [showPassword, setShowPassword]       = useState(false)
  const [showConfirmPassword, setShowConfirm] = useState(false)
  const { mutate: register_, isPending }      = useRegister()

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const onSubmit = ({ name, cpf, gender, email, password }: FormData) =>
    register_({ name, cpf, gender, email, password, currency: 'BRL', timezone: 'America/Sao_Paulo' })

  return (
    <div className="bg-slate-900/80 backdrop-blur-xl border border-slate-700/50 rounded-2xl p-8 shadow-2xl">

      <div className="text-center mb-8">
        <div className="inline-flex items-center justify-center w-14 h-14 bg-violet-500/20 border border-violet-500/30 rounded-2xl mb-4">
          <Sparkles className="text-violet-400" size={26} />
        </div>
        <h1 className="text-2xl font-bold text-white">Criar sua conta</h1>
        <p className="text-slate-400 text-sm mt-1">Comece a controlar suas finanças hoje</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

        <div className="space-y-2">
          <Label htmlFor="name" className="text-slate-300 text-sm">Nome completo</Label>
          <Input
            id="name"
            placeholder="Seu nome completo"
            {...register('name')}
            className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-violet-500 focus:ring-violet-500/20 h-11"
          />
          {errors.name && <p className="text-red-400 text-xs">{errors.name.message}</p>}
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label htmlFor="cpf" className="text-slate-300 text-sm">CPF</Label>
            <Input
              id="cpf"
              placeholder="000.000.000-00"
              value={watch('cpf') ?? ''}
              onChange={e => setValue('cpf', maskCpf(e.target.value), { shouldValidate: true })}
              className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-violet-500 focus:ring-violet-500/20 h-11"
            />
            {errors.cpf && <p className="text-red-400 text-xs">{errors.cpf.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="gender" className="text-slate-300 text-sm">Gênero</Label>
            <select
              id="gender"
              {...register('gender')}
              className="w-full h-11 px-3 rounded-md bg-slate-800/50 border border-slate-700 text-white focus:border-violet-500 focus:outline-none focus:ring-1 focus:ring-violet-500/20 text-sm"
            >
              <option value="" className="bg-slate-800">Selecione</option>
              <option value="Male" className="bg-slate-800">Masculino</option>
              <option value="Female" className="bg-slate-800">Feminino</option>
            </select>
            {errors.gender && <p className="text-red-400 text-xs">{errors.gender.message}</p>}
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email" className="text-slate-300 text-sm">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="seu@email.com"
            {...register('email')}
            className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-violet-500 focus:ring-violet-500/20 h-11"
          />
          {errors.email && <p className="text-red-400 text-xs">{errors.email.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="password" className="text-slate-300 text-sm">Senha</Label>
          <div className="relative">
            <Input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              {...register('password')}
              className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-violet-500 focus:ring-violet-500/20 h-11 pr-11"
            />
            <button type="button" onClick={() => setShowPassword(p => !p)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.password && <p className="text-red-400 text-xs">{errors.password.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword" className="text-slate-300 text-sm">Confirmar senha</Label>
          <div className="relative">
            <Input
              id="confirmPassword"
              type={showConfirmPassword ? 'text' : 'password'}
              placeholder="••••••••"
              {...register('confirmPassword')}
              className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-violet-500 focus:ring-violet-500/20 h-11 pr-11"
            />
            <button type="button" onClick={() => setShowConfirm(p => !p)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
              {showConfirmPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.confirmPassword && <p className="text-red-400 text-xs">{errors.confirmPassword.message}</p>}
        </div>

        <Button
          type="submit"
          disabled={isPending}
          className="w-full h-11 bg-violet-600 hover:bg-violet-500 text-white font-medium rounded-xl transition-all duration-200 shadow-lg shadow-violet-500/25 mt-2"
        >
          {isPending
            ? <><Loader2 size={16} className="animate-spin mr-2" />Criando conta...</>
            : 'Criar conta'
          }
        </Button>
      </form>

      <p className="text-center text-slate-400 text-sm mt-6">
        Já tem uma conta?{' '}
        <Link to="/login" className="text-violet-400 hover:text-violet-300 font-medium transition-colors">
          Entrar
        </Link>
      </p>
    </div>
  )
}