import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { Mail, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useForgotPassword } from '../api/useAuth'

const schema = z.object({
  email: z.string().email('Email inválido.'),
})

type FormData = z.infer<typeof schema>

export default function ForgotPasswordPage() {
  const { mutate: forgotPassword, isPending } = useForgotPassword()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  const onSubmit = (data: FormData) => forgotPassword(data)

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="text-center mb-8">
        <div className="w-12 h-12 bg-indigo-500/20 rounded-2xl flex items-center justify-center mx-auto mb-4">
          <Mail size={24} className="text-indigo-400" />
        </div>
        <h1 className="text-2xl font-bold text-white">Esqueceu a senha?</h1>
        <p className="text-slate-400 mt-2 text-sm">
          Informe seu email e enviaremos um link para redefinir sua senha.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email" className="text-slate-300">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="seu@email.com"
            autoComplete="email"
            className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500"
            {...register('email')}
          />
          {errors.email && (
            <p className="text-red-400 text-xs">{errors.email.message}</p>
          )}
        </div>

        <Button
          type="submit"
          disabled={isPending}
          className="w-full bg-indigo-600 hover:bg-indigo-500 text-white font-semibold py-2.5"
        >
          {isPending ? 'Enviando...' : 'Enviar link de redefinição'}
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