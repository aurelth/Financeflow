import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
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

export default function SubcategoryForm({ subcategory, isPending, onSubmit, onCancel }: SubcategoryFormProps) {
  const isEditing = !!subcategory

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: subcategory?.name ?? '' },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1.5">
        <label
          className="block text-sm"
          style={{ color: 'var(--ff-text-secondary)' }}
        >
          Nome
        </label>
        <input
          {...register('name')}
          placeholder="Ex: Restaurante"
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />
        {errors.name && (
          <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.name.message}</p>
        )}
      </div>

      <div className="flex gap-3">
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
            : isEditing ? 'Salvar alterações' : 'Adicionar'
          }
        </button>
      </div>
    </form>
  )
}