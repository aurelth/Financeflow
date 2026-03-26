import { Loader2, TriangleAlert } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { Subcategory } from '../types/category.types'

interface DeleteSubcategoryDialogProps {
  subcategory: Subcategory
  isPending:   boolean
  onConfirm:   () => void
  onCancel:    () => void
}

export default function DeleteSubcategoryDialog({
  subcategory,
  isPending,
  onConfirm,
  onCancel,
}: DeleteSubcategoryDialogProps) {
  return (
    <div className="space-y-5">
      <div className="flex gap-3 p-4 bg-red-500/10 border border-red-500/20 rounded-xl">
        <TriangleAlert size={18} className="text-red-400 flex-shrink-0 mt-0.5" />
        <div className="space-y-1">
          <p className="text-slate-200 text-sm font-medium">
            Remover <span className="text-white font-semibold">"{subcategory.name}"</span>?
          </p>
          <p className="text-slate-400 text-xs leading-relaxed">
            Se houver transações vinculadas, a subcategoria será desativada e preservada
            no histórico. Caso contrário, será removida permanentemente.
          </p>
        </div>
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
          onClick={onConfirm}
          disabled={isPending}
          className="flex-1 bg-red-600 hover:bg-red-500 text-white h-10"
        >
          {isPending
            ? <><Loader2 size={15} className="animate-spin mr-2" />Removendo...</>
            : 'Sim, remover'
          }
        </Button>
      </div>
    </div>
  )
}