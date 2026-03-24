import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import CategoryForm from './CategoryForm'
import SubcategoryForm from './SubcategoryForm'
import DeleteCategoryDialog from './DeleteCategoryDialog'
import DeleteSubcategoryDialog from './DeleteSubcategoryDialog'
import {
  useCreateCategory,
  useUpdateCategory,
  useDeleteCategory,
  useCreateSubcategory,
  useUpdateSubcategory,
  useDeleteSubcategory,
} from '../api/useCategories'
import type { Category, Subcategory } from '../types/category.types'

// Tipos de modal

export type ModalState =
  | { type: 'create-category' }
  | { type: 'edit-category';         category: Category }
  | { type: 'delete-category';       category: Category }
  | { type: 'create-subcategory';    category: Category }
  | { type: 'edit-subcategory';      category: Category; subcategory: Subcategory }
  | { type: 'delete-subcategory';    category: Category; subcategory: Subcategory }
  | null

interface CategoryModalProps {
  state:    ModalState
  onClose:  () => void
}

// Títulos por tipo

const TITLES: Record<NonNullable<ModalState>['type'], string> = {
  'create-category':    'Nova categoria',
  'edit-category':      'Editar categoria',
  'delete-category':    'Remover categoria',
  'create-subcategory': 'Nova subcategoria',
  'edit-subcategory':   'Editar subcategoria',
  'delete-subcategory': 'Remover subcategoria',
}

export default function CategoryModal({ state, onClose }: CategoryModalProps) {
  // Mutations

  const createCategory = useCreateCategory()
  const updateCategory = useUpdateCategory(
    state?.type === 'edit-category' ? state.category.id : ''
  )
  const deleteCategory = useDeleteCategory()

  const createSubcategory = useCreateSubcategory(
    state?.type === 'create-subcategory' ? state.category.id : ''
  )
  const updateSubcategory = useUpdateSubcategory(
    state?.type === 'edit-subcategory' ? state.category.id : '',
    state?.type === 'edit-subcategory' ? state.subcategory.id : ''
  )
  const deleteSubcategory = useDeleteSubcategory(
    state?.type === 'delete-subcategory' ? state.category.id : ''
  )

  // Handlers

  const handleSuccess = () => onClose()

  // Conteúdo por tipo

  const renderContent = () => {
    if (!state) return null

    switch (state.type) {

      case 'create-category':
        return (
          <CategoryForm
            isPending={createCategory.isPending}
            onCancel={onClose}
            onSubmit={data =>
              createCategory.mutate(data, { onSuccess: handleSuccess })
            }
          />
        )

      case 'edit-category':
        return (
          <CategoryForm
            category={state.category}
            isPending={updateCategory.isPending}
            onCancel={onClose}
            onSubmit={data =>
              updateCategory.mutate(data, { onSuccess: handleSuccess })
            }
          />
        )

      case 'delete-category':
        return (
          <DeleteCategoryDialog
            category={state.category}
            isPending={deleteCategory.isPending}
            onCancel={onClose}
            onConfirm={() =>
              deleteCategory.mutate(state.category.id, { onSuccess: handleSuccess })
            }
          />
        )

      case 'create-subcategory':
        return (
          <SubcategoryForm
            isPending={createSubcategory.isPending}
            onCancel={onClose}
            onSubmit={data =>
              createSubcategory.mutate(data, { onSuccess: handleSuccess })
            }
          />
        )

      case 'edit-subcategory':
        return (
          <SubcategoryForm
            subcategory={state.subcategory}
            isPending={updateSubcategory.isPending}
            onCancel={onClose}
            onSubmit={data =>
              updateSubcategory.mutate(data, { onSuccess: handleSuccess })
            }
          />
        )

      case 'delete-subcategory':
        return (
          <DeleteSubcategoryDialog
            subcategory={state.subcategory}
            isPending={deleteSubcategory.isPending}
            onCancel={onClose}
            onConfirm={() =>
              deleteSubcategory.mutate(state.subcategory.id, { onSuccess: handleSuccess })
            }
          />
        )
    }
  }

  return (
    <Dialog open={!!state} onOpenChange={open => !open && onClose()}>
      <DialogContent className="bg-slate-900 border-slate-800 text-white sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-slate-100">
            {state ? TITLES[state.type] : ''}
          </DialogTitle>
        </DialogHeader>
        {renderContent()}
      </DialogContent>
    </Dialog>
  )
}