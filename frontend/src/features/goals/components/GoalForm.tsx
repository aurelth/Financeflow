import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import type { GoalProgressResultDto } from '../types/goal.types'

const EMOJIS = [
  '🎯', '✈️', '🚗', '🏠', '💻', '📱', '🎓', '💍',
  '🏖️', '🚀', '💰', '🏋️', '🎸', '🌍', '🛒', '🏥',
  '🎁', '🐶', '🌱', '⭐', '🏆', '🎨', '📚', '🛡️',
]

const schema = z.object({
  name:                z.string().min(2, 'Mínimo 2 caracteres').max(100, 'Máximo 100 caracteres'),
  targetAmount:        z.number({ error: 'Valor obrigatório' }).positive('Deve ser maior que zero'),
  monthlyContribution: z.number({ error: 'Valor obrigatório' }).positive('Deve ser maior que zero'),
  deadline:            z.string().min(1, 'Prazo obrigatório'),
  emoji:               z.string().min(1, 'Selecione um emoji'),
}).refine(d => d.monthlyContribution <= d.targetAmount, {
  message: 'A contribuição mensal não pode ser maior que o valor alvo',
  path:    ['monthlyContribution'],
}).refine(d => new Date(d.deadline) > new Date(), {
  message: 'O prazo deve ser uma data futura',
  path:    ['deadline'],
})

type FormData = z.infer<typeof schema>

interface GoalFormProps {
  goal?:      GoalProgressResultDto
  onSubmit:   (data: FormData) => void
  isPending:  boolean
  onCancel:   () => void
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
}

const labelStyle: React.CSSProperties = {
  display:      'block',
  fontSize:     '13px',
  color:        'var(--ff-text-secondary)',
  marginBottom: '6px',
}

export default function GoalForm({ goal, onSubmit, isPending, onCancel }: GoalFormProps) {
  const isEditing = !!goal

  const { register, handleSubmit, control, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      name:                goal?.name                ?? '',
      targetAmount:        goal?.targetAmount        ?? undefined,
      monthlyContribution: goal?.monthlyContribution ?? undefined,
      deadline:            goal?.deadline
        ? new Date(goal.deadline).toISOString().split('T')[0]
        : '',
      emoji: goal?.emoji ?? '🎯',
    },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

      {/* Emoji */}
      <div className="space-y-1.5">
        <label style={labelStyle}>Emoji</label>
        <Controller
          name="emoji"
          control={control}
          render={({ field }) => (
            <div className="flex flex-wrap gap-1.5">
              {EMOJIS.map(emoji => (
                <button
                  key={emoji}
                  type="button"
                  onClick={() => field.onChange(emoji)}
                  className="w-9 h-9 rounded-lg text-lg flex items-center justify-center transition-all"
                  style={field.value === emoji
                    ? { background: 'var(--ff-emerald-subtle)', outline: '1px solid rgba(16,185,129,0.4)', transform: 'scale(1.1)' }
                    : { background: 'var(--ff-bg-elevated)' }
                  }
                  onMouseEnter={e => { if (field.value !== emoji) e.currentTarget.style.background = '#222222' }}
                  onMouseLeave={e => { if (field.value !== emoji) e.currentTarget.style.background = 'var(--ff-bg-elevated)' }}
                >
                  {emoji}
                </button>
              ))}
            </div>
          )}
        />
        {errors.emoji && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.emoji.message}</p>}
      </div>

      {/* Nome */}
      <div className="space-y-1.5">
        <label style={labelStyle}>Nome da meta</label>
        <input
          {...register('name')}
          placeholder="Ex: Viagem para Europa"
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />
        {errors.name && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.name.message}</p>}
      </div>

      {/* Valor alvo e contribuição mensal */}
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <label style={labelStyle}>Valor alvo (R$)</label>
          <input
            {...register('targetAmount', { valueAsNumber: true })}
            type="number"
            step="0.01"
            placeholder="5000"
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.targetAmount && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.targetAmount.message}</p>}
        </div>

        <div className="space-y-1.5">
          <label style={labelStyle}>Contribuição mensal (R$)</label>
          <input
            {...register('monthlyContribution', { valueAsNumber: true })}
            type="number"
            step="0.01"
            placeholder="400"
            style={inputStyle}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
          {errors.monthlyContribution && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.monthlyContribution.message}</p>}
        </div>
      </div>

      {/* Prazo */}
      <div className="space-y-1.5">
        <label style={labelStyle}>Prazo</label>
        <input
          {...register('deadline')}
          type="date"
          style={{ ...inputStyle, colorScheme: 'dark' }}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />
        {errors.deadline && <p className="text-xs" style={{ color: 'var(--ff-expense)' }}>{errors.deadline.message}</p>}
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
            : isEditing ? 'Salvar alterações' : 'Criar meta'
          }
        </button>
      </div>
    </form>
  )
}