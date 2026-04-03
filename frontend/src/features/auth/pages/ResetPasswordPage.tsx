import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useSearchParams } from 'react-router-dom'
import { KeyRound, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useResetPassword } from '../api/useAuth'

const schema = z.object({
  newPassword: z
    .string()
    .min(8, 'Mínimo 8 caracteres.')
    .regex(/[A-Z]/, 'Deve conter pelo menos uma letra maiúscula.')
    .regex(/[0-9]/, 'Deve conter pelo menos um número.')
    .regex(/[^a-zA-Z0-9]/, 'Deve conter pelo menos um símbolo.'),
  confirmPassword: z.string(),
}).refine(data => data.newPassword === data.confirmPassword, {
  message: 'As senhas não coincidem.',
  path:    ['confirmPassword'],
})

type FormData = z.infer<typeof schema>

export default function ResetPasswordPage() {
  const [searchParams]                        = useSearchParams()
  const token                                 = searchParams.get('token') ?? ''
  const { mutate: resetPassword, isPending }  = useResetPassword()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  const onSubmit = (data: FormData) =>
    resetPassword({
      token,
      newPassword:     data.newPassword,
      confirmPassword: data.confirmPassword,
    })

  if (!token) {
    return (
      <div className="w-full max-w-md mx-auto text-center">
        <p className="text-red-400 mb-4">Link de redefinição inválido ou expirado.</p>
        <Link to="/login" className="text-indigo-400 hover:text-indigo-300 text-sm">
          Voltar para o login
        </Link>
      </div>
    )
  }

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="text-center mb-8">
        <div className="w-12 h-12 bg-indigo-500/20 rounded-2xl flex items-center justify-center mx-auto mb-4">
          <KeyRound size={24} className="text-indigo-400" />
        </div>
        <h1 className="text-2xl font-bold text-white">Redefinir senha</h1>
        <p className="text-slate-400 mt-2 text-sm">
          Escolha uma nova senha para a sua conta.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="newPassword" className="text-slate-300">Nova senha</Label>
          <Input
            id="newPassword"
            type="password"
            placeholder="••••••••"
            autoComplete="new-password"
            className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500"
            {...register('newPassword')}
          />
          {errors.newPassword && (
            <p className="text-red-400 text-xs">{errors.newPassword.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword" className="text-slate-300">Confirmar senha</Label>
          <Input
            id="confirmPassword"
            type="password"
            placeholder="••••••••"
            autoComplete="new-password"
            className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500"
            {...register('confirmPassword')}
          />
          {errors.confirmPassword && (
            <p className="text-red-400 text-xs">{errors.confirmPassword.message}</p>
          )}
        </div>

        <Button
          type="submit"
          disabled={isPending}
          className="w-full bg-indigo-600 hover:bg-indigo-500 text-white font-semibold py-2.5"
        >
          {isPending ? 'Redefinindo...' : 'Redefinir senha'}
        </Button>
      </form>

      <div className="mt-6 text-center">
        <Link
          to="/login"
          className="inline-flex items-center gap-2 text-slate-400 hover:text-slate-200 text-sm transition-colors"
        >
          <ArrowLeft size={14} />
          Voltar para o login
        </Link>
      </div>
    </div>
  )
}