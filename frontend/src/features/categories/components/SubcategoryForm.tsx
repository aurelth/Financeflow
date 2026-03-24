import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { Subcategory } from '../types/category.types'

const schema = z.object({
  name: z.string().min(2, 'Mínimo 2 caracteres').max(100, 'Máximo 100 caracteres'),
})

type FormData = z.infer<typeof schema>

interface SubcategoryFormProps {
  subcategory?: Subcategory
  isPending:    boolean
  onSubmit:     (data: FormData) => void
  onCancel:     () => void
}

export default function SubcategoryForm({
  subcategory,
  isPending,
  onSubmit,
  onCancel,
}: SubcategoryFormProps) {
  const isEditing = !!subcategory

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: subcategory?.name ?? '' },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label className="text-slate-300 text-sm">Nome</Label>
        <Input
          {...register('name')}
          placeholder="Ex: Restaurante"
          className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500 h-10"
        />
        {errors.name && <p className="text-red-400 text-xs">{errors.name.message}</p>}
      </div>

      <div className="flex gap-3">
        <Button
          type="button"
          onClick={onCancel}
          className="flex-1 bg-slate-800 hover:bg-slate-700 text-slate-300 border-0 h-10"
        >
          Cancelar
        </Button>
        <Button
          type="submit"
          disabled={isPending}
          className="flex-1 bg-indigo-600 hover:bg-indigo-500 text-white h-10"
        >
          {isPending
            ? <><Loader2 size={15} className="animate-spin mr-2" />Salvando...</>
            : isEditing ? 'Salvar alterações' : 'Adicionar'
          }
        </Button>
      </div>
    </form>
  )
}