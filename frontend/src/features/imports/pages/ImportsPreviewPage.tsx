import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, CheckCircle, Loader2, FileText } from 'lucide-react'
import { useImportPreview, useConfirmImport } from '../api/useImports'
import { useCategories } from '@/features/categories/api/useCategories'
import ImportPreviewTable from '../components/ImportPreviewTable'
import type { BankImportTransactionDto } from '../types/imports.types'
import { toast } from 'sonner'

export default function ImportsPreviewPage() {
  const { id }     = useParams<{ id: string }>()
  const navigate   = useNavigate()

  const { data: preview, isLoading } = useImportPreview(id ?? null)
  const { data: categories = [] }    = useCategories()
  const { mutate: confirm, isPending } = useConfirmImport(id ?? '')

  const [transactions, setTransactions] = useState<BankImportTransactionDto[]>([])

  useEffect(() => {
    if (preview?.transactions) {
      setTransactions(preview.transactions)
    }
  }, [preview])

  function handleConfirm() {
    const selectedIds = transactions
      .filter(t => t.isSelected && !t.isDuplicate)
      .map(t => t.id)

    if (selectedIds.length === 0) {
      toast.error('Selecione ao menos uma transação para importar.')
      return
    }

    confirm(
      { selectedTransactionIds: selectedIds },
      {
        onSuccess: () => {
          toast.success(`${selectedIds.length} transações importadas com sucesso!`)
          navigate('/imports')
        },
        onError: () => {
          toast.error('Erro ao confirmar importação. Tente novamente.')
        },
      }
    )
  }

  const selectedCount  = transactions.filter(t => t.isSelected).length
  const duplicateCount = transactions.filter(t => t.isDuplicate).length

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/imports')}
            className="p-2 rounded-xl transition-colors"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color = 'var(--ff-text-primary)'
              e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }}
          >
            <ArrowLeft size={18} />
          </button>
          <div>
            <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
              Preview da importação
            </h1>
            <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
              {preview?.fileName ?? '...'}
            </p>
          </div>
        </div>

        {!isLoading && transactions.length > 0 && (
          <button
            onClick={handleConfirm}
            disabled={isPending || selectedCount === 0}
            className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            onMouseEnter={e => {
              if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)'
            }}
            onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
          >
            {isPending
              ? <><Loader2 size={15} className="animate-spin" /> A importar...</>
              : <><CheckCircle size={15} /> Confirmar ({selectedCount})</>
            }
          </button>
        )}
      </div>

      {/* Resumo do ficheiro */}
      {preview && (
        <div
          className="flex items-center gap-6 px-5 py-4 rounded-xl"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <div className="flex items-center gap-3">
            <div
              className="w-9 h-9 rounded-xl flex items-center justify-center"
              style={{ background: 'var(--ff-bg-elevated)' }}
            >
              <FileText size={16} style={{ color: 'var(--ff-emerald)' }} />
            </div>
            <div>
              <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
                {preview.fileName}
              </p>
              <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                {preview.totalRecords} transações encontradas
              </p>
            </div>
          </div>

          {duplicateCount > 0 && (
            <div
              className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs"
              style={{ background: 'rgba(248,113,113,0.1)', color: '#f87171' }}
            >
              {duplicateCount} duplicada{duplicateCount > 1 ? 's' : ''} detetada{duplicateCount > 1 ? 's' : ''}
            </div>
          )}
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {/* Tabela */}
      {!isLoading && transactions.length > 0 && (
        <ImportPreviewTable
          transactions={transactions}
          categories={categories}
          onChange={setTransactions}
        />
      )}

      {/* Estado vazio */}
      {!isLoading && transactions.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div
            className="w-14 h-14 rounded-2xl flex items-center justify-center"
            style={{ background: 'var(--ff-bg-card)' }}
          >
            <FileText size={24} style={{ color: 'var(--ff-text-muted)' }} />
          </div>
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma transação encontrada neste ficheiro
          </p>
        </div>
      )}
    </div>
  )
}