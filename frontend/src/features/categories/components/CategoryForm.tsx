import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import ColorPicker from './ColorPicker'
import IconPicker from './IconPicker'
import { TransactionType, type Category } from '../types/category.types'

const schema = z.object({
  name:  z.string().min(2, 'Mínimo 2 caracteres').max(100, 'Máximo 100 caracteres'),
  icon:  z.string().min(1, 'Selecione um ícone'),
  color: z.string().regex(/^#([A-Fa-f0-9]{6})$/, 'Cor inválida'),
  type:  z.nativeEnum(TransactionType),
})

type FormData = z.infer<typeof schema>

interface CategoryFormProps {
  category?:  Category        // se definido, modo edição
  onSubmit:   (data: FormData) => void
  isPending:  boolean
  onCancel:   () => void
}

export default function CategoryForm({
  category,
  onSubmit,
  isPending,
  onCancel,
}: CategoryFormProps) {
  const isEditing = !!category

  const { register, handleSubmit, control, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      name:  category?.name  ?? '',
      icon:  category?.icon  ?? '📁',
      color: category?.color ?? '#6366f1',
      type:  category?.type  ?? TransactionType.Expense,
    },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

      {/* Nome */}
      <div className="space-y-2">
        <Label className="text-slate-300 text-sm">Nome</Label>
        <Input
          {...register('name')}
          placeholder="Ex: Alimentação"
          className="bg-slate-800/50 border-slate-700 text-white placeholder:text-slate-500 focus:border-indigo-500 h-10"
        />
        {errors.name && <p className="text-red-400 text-xs">{errors.name.message}</p>}
      </div>

      {/* Tipo — apenas no modo criação */}
      {!isEditing && (
        <div className="space-y-2">
          <Label className="text-slate-300 text-sm">Tipo</Label>
          <Controller
            name="type"
            control={control}
            render={({ field }) => (
              <div className="flex gap-2">
                {[
                  { label: 'Receita', value: TransactionType.Income  },
                  { label: 'Despesa', value: TransactionType.Expense },
                ].map(opt => (
                  <button
                    key={opt.value}
                    type="button"
                    onClick={() => field.onChange(opt.value)}
                    className={`flex-1 py-2 rounded-xl text-sm font-medium transition-all duration-200 ${
                      field.value === opt.value
                        ? opt.value === TransactionType.Income
                          ? 'bg-emerald-500/20 text-emerald-400 ring-1 ring-emerald-500/40'
                          : 'bg-red-500/20 text-red-400 ring-1 ring-red-500/40'
                        : 'bg-slate-800 text-slate-400 hover:bg-slate-700'
                    }`}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            )}
          />
        </div>
      )}

      {/* Ícone */}
      <div className="space-y-2">
        <Label className="text-slate-300 text-sm">Ícone</Label>
        <Controller
          name="icon"
          control={control}
          render={({ field }) => (
            <IconPicker value={field.value} onChange={field.onChange} />
          )}
        />
        {errors.icon && <p className="text-red-400 text-xs">{errors.icon.message}</p>}
      </div>

      {/* Cor */}
      <div className="space-y-2">
        <Label className="text-slate-300 text-sm">Cor</Label>
        <Controller
          name="color"
          control={control}
          render={({ field }) => (
            <ColorPicker value={field.value} onChange={field.onChange} />
          )}
        />
        {errors.color && <p className="text-red-400 text-xs">{errors.color.message}</p>}
      </div>

      {/* Ações */}
      <div className="flex gap-3 pt-2">
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
            : isEditing ? 'Salvar alterações' : 'Criar categoria'
          }
        </Button>
      </div>
    </form>
  )
}