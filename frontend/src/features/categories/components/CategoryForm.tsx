import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
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
  category?:  Category
  onSubmit:   (data: FormData) => void
  isPending:  boolean
  onCancel:   () => void
}

// Estilos com tokens da nova paleta
const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '10px',
  padding:      '9px 14px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

const labelStyle: React.CSSProperties = {
  display:      'block',
  fontSize:     '13px',
  color:        'var(--ff-text-secondary)',
  marginBottom: '6px',
}

export default function CategoryForm({ category, onSubmit, isPending, onCancel }: CategoryFormProps) {
  const isEditing = !!category

  const { register, handleSubmit, control, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      name:  category?.name  ?? '',
      icon:  category?.icon  ?? '📁',
      color: category?.color ?? '#10b981',
      type:  category?.type  ?? TransactionType.Expense,
    },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

      {/* Nome */}
      <div className="space-y-1.5">
        <label style={labelStyle}>Nome</label>
        <input
          {...register('name')}
          placeholder="Ex: Alimentação"
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />
        {errors.name && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.name.message}</p>}
      </div>

      {/* Tipo — apenas no modo criação */}
      {!isEditing && (
        <div className="space-y-1.5">
          <label style={labelStyle}>Tipo</label>
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
                    className="flex-1 py-2 rounded-xl text-sm font-medium transition-all duration-200"
                    style={field.value === opt.value
                      ? opt.value === TransactionType.Income
                        ? { background: 'rgba(16,185,129,0.1)', color: 'var(--ff-income)', border: '1px solid rgba(16,185,129,0.4)' }
                        : { background: 'rgba(244,63,94,0.1)', color: 'var(--ff-expense)', border: '1px solid rgba(244,63,94,0.4)' }
                      : { background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-muted)', border: '1px solid var(--ff-border)' }
                    }
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
      <div className="space-y-1.5">
        <label style={labelStyle}>Ícone</label>
        <Controller
          name="icon"
          control={control}
          render={({ field }) => <IconPicker value={field.value} onChange={field.onChange} />}
        />
        {errors.icon && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.icon.message}</p>}
      </div>

      {/* Cor */}
      <div className="space-y-1.5">
        <label style={labelStyle}>Cor</label>
        <Controller
          name="color"
          control={control}
          render={({ field }) => <ColorPicker value={field.value} onChange={field.onChange} />}
        />
        {errors.color && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.color.message}</p>}
      </div>

      {/* Ações */}
      <div className="flex gap-3 pt-2">
        <button
          type="button"
          onClick={onCancel}
          className="flex-1 h-10 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
          onMouseEnter={e => (e.currentTarget.style.background = '#222222')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={isPending}
          className="flex-1 h-10 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
        >
          {isPending
            ? <span className="flex items-center justify-center gap-2"><Loader2 size={15} className="animate-spin" />Salvando...</span>
            : isEditing ? 'Salvar alterações' : 'Criar categoria'
          }
        </button>
      </div>
    </form>
  )
}