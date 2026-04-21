import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import GoalForm from './GoalForm'
import DeleteGoalDialog from './DeleteGoalDialog'
import { useCreateGoal, useUpdateGoal, useDeleteGoal } from '../api/useGoals'
import type { GoalProgressResultDto } from '../types/goal.types'

export type GoalModalState =
  | { type: 'create' }
  | { type: 'edit';   goal: GoalProgressResultDto }
  | { type: 'delete'; goal: GoalProgressResultDto }
  | null

interface GoalModalProps {
  state:   GoalModalState
  onClose: () => void
}

const TITLES: Record<NonNullable<GoalModalState>['type'], string> = {
  create: 'Nova meta',
  edit:   'Editar meta',
  delete: 'Remover meta',
}

export default function GoalModal({ state, onClose }: GoalModalProps) {
  const createGoal = useCreateGoal()
  const updateGoal = useUpdateGoal(state?.type === 'edit' ? state.goal.id : '')
  const deleteGoal = useDeleteGoal()

  const renderContent = () => {
    if (!state) return null
    switch (state.type) {
      case 'create':
        return (
          <GoalForm
            isPending={createGoal.isPending}
            onCancel={onClose}
            onSubmit={data => createGoal.mutate(
              { ...data, deadline: new Date(data.deadline).toISOString() },
              { onSuccess: onClose })}
          />
        )
      case 'edit':
        return (
          <GoalForm
            goal={state.goal}
            isPending={updateGoal.isPending}
            onCancel={onClose}
            onSubmit={data => updateGoal.mutate(
              { ...data, deadline: new Date(data.deadline).toISOString() },
              { onSuccess: onClose })}
          />
        )
      case 'delete':
        return (
          <DeleteGoalDialog
            goal={state.goal}
            isPending={deleteGoal.isPending}
            onCancel={onClose}
            onConfirm={() => deleteGoal.mutate(state.goal.id, { onSuccess: onClose })}
          />
        )
    }
  }

  return (
    <Dialog open={!!state} onOpenChange={open => !open && onClose()}>
      <DialogContent
        className="sm:max-w-md"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-primary)' }}
      >
        <DialogHeader>
          <DialogTitle style={{ color: 'var(--ff-text-primary)' }}>
            {state ? TITLES[state.type] : ''}
          </DialogTitle>
        </DialogHeader>
        {renderContent()}
      </DialogContent>
    </Dialog>
  )
}