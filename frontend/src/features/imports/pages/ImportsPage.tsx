import { useState } from 'react'
import { Upload, FileText, CheckCircle, XCircle, Clock, Loader2, RefreshCw, Trash2 } from 'lucide-react'
import { useImports, useUploadOFX, useDeleteImport } from '../api/useImports'
import OFXUploadZone from '../components/OFXUploadZone'
import CsvImport from '../components/CsvImport'
import PasteImport from '../components/PasteImport'
import type { BankImportDto, BankImportStatus } from '../types/imports.types'
import { toast } from 'sonner'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

type ImportMode = 'ofx' | 'csv' | 'paste'

function StatusBadge({ status }: { status: BankImportStatus }) {
  const map: Record<string, { label: string; color: string; bg: string; icon: React.ReactNode }> = {
    'Pending':    { label: 'Pendente',    color: '#facc15', bg: 'rgba(250,204,21,0.1)',  icon: <Clock size={12} /> },
    'Processing': { label: 'A processar', color: '#60a5fa', bg: 'rgba(96,165,250,0.1)',  icon: <Loader2 size={12} className="animate-spin" /> },
    'Completed':  { label: 'Concluído',   color: '#34d399', bg: 'rgba(52,211,153,0.1)',  icon: <CheckCircle size={12} /> },
    'Failed':     { label: 'Erro',        color: '#f87171', bg: 'rgba(248,113,113,0.1)', icon: <XCircle size={12} /> },
    '1':          { label: 'Pendente',    color: '#facc15', bg: 'rgba(250,204,21,0.1)',  icon: <Clock size={12} /> },
    '2':          { label: 'A processar', color: '#60a5fa', bg: 'rgba(96,165,250,0.1)',  icon: <Loader2 size={12} className="animate-spin" /> },
    '3':          { label: 'Concluído',   color: '#34d399', bg: 'rgba(52,211,153,0.1)',  icon: <CheckCircle size={12} /> },
    '4':          { label: 'Erro',        color: '#f87171', bg: 'rgba(248,113,113,0.1)', icon: <XCircle size={12} /> },
  }

  const entry = map[String(status)]
  if (!entry) return null

  const { label, color, bg, icon } = entry
  return (
    <span
      className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium"
      style={{ color, background: bg }}
    >
      {icon}{label}
    </span>
  )
}

interface ImportHistoryRowProps {
  item: BankImportDto
  onPreview: (id: string) => void
  onDelete: (id: string) => void
  isDeleting: boolean
}

function ImportHistoryRow({ item, onPreview, onDelete, isDeleting }: ImportHistoryRowProps) {
  const date = new Date(`${item.createdAt}Z`).toLocaleDateString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })

  const statusStr  = String(item.status)
  const isCompleted = statusStr === 'Completed' || statusStr === '3'
  const isPending   = statusStr === 'Pending'   || statusStr === '1'

  return (
    <div
      className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between px-4 sm:px-5 py-4 rounded-xl transition-colors"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center gap-4 min-w-0">
        <div
          className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0"
          style={{ background: 'var(--ff-bg-elevated)' }}
        >
          <FileText size={16} style={{ color: 'var(--ff-emerald)' }} />
        </div>
        <div className="min-w-0">
          <p className="text-sm font-medium break-all" style={{ color: 'var(--ff-text-primary)' }}>
            {item.fileName}
          </p>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            {date} · {item.totalRecords} transações{item.accountName ? ` · ${item.accountName}` : ''}
          </p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3 sm:flex-shrink-0">
        {isCompleted && (
          <div className="w-full sm:w-auto sm:text-right">
            <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
              <span style={{ color: '#34d399' }}>{item.imported} importadas</span>
              {item.duplicates > 0 && <> · {item.duplicates} duplicadas</>}
              {item.errors > 0 && <> · {item.errors} erros</>}
            </p>
          </div>
        )}

        <StatusBadge status={item.status} />

        {(isCompleted || isPending) && (
          <button
            onClick={() => onPreview(item.id)}
            className="h-9 sm:h-8 px-3 rounded-xl text-xs font-medium transition-colors whitespace-nowrap"
            style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-secondary)')}
          >
            Ver preview
          </button>
        )}

        {/* Botão de eliminar */}
        <button
          onClick={() => onDelete(item.id)}
          disabled={isDeleting}
          title="Excluir importação"
          className="p-2 rounded-xl transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => {
            e.currentTarget.style.color = '#f87171'
            e.currentTarget.style.background = 'rgba(248,113,113,0.1)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.color = 'var(--ff-text-muted)'
            e.currentTarget.style.background = 'transparent'
          }}
        >
          {isDeleting
            ? <Loader2 size={15} className="animate-spin" />
            : <Trash2 size={15} />
          }
        </button>
      </div>
    </div>
  )
}

export default function ImportsPage() {
  const navigate                                    = useNavigate()
  const { t }                                       = useTranslation('accounts')
  const { t: tImports }                             = useTranslation('imports')
  const { data: imports = [], isLoading, refetch } = useImports()
  const { mutate: upload, isPending }              = useUploadOFX()
  const { mutate: deleteImport, isPending: isDeleting } = useDeleteImport()
  const [uploading, setUploading]                  = useState(false)
  const [mode, setMode]                          = useState<ImportMode>('ofx')
  // Texto colado que já virou arquivo: segue para o formato detectado já escolhido
  const [pastedFile, setPastedFile]                = useState<File | undefined>()

  function selectMode(next: ImportMode) {
    setPastedFile(undefined)
    setMode(next)
  }
  const [deletingId, setDeletingId]                = useState<string | null>(null)

  // Informa como a conta foi decidida, para o utilizador poder corrigir antes de confirmar, e abre o preview
  function openPreview(result: BankImportDto) {
    const name = result.accountName ?? ''
    if (result.accountResolution === 'Recognized')
      toast.info(t('import.recognized', { name }))
    else if (result.accountResolution === 'Learned')
      toast.info(t('import.learned', { name }))
    else if (result.accountResolution === 'Defaulted')
      toast.info(t('import.defaulted', { name }))
    navigate(`/imports/${result.id}/preview`)
  }

  function handleFileSelect(file: File, accountId?: string) {
    setUploading(true)
    upload({ file, accountId }, {
      onSuccess: (result) => {
        setUploading(false)
        toast.success('Arquivo enviado! Processando...')
        openPreview(result)
      },
      onError: () => {
        setUploading(false)
        toast.error('Erro ao enviar o arquivo. Verifique se é um OFX válido.')
      },
    })
  }

  // Handler de eliminar
  function handleDelete(id: string) {
    setDeletingId(id)
    deleteImport(id, {
      onSuccess: () => {
        setDeletingId(null)
        toast.success('Importação excluída com sucesso.')
      },
      onError: () => {
        setDeletingId(null)
        toast.error('Erro ao excluir a importação.')
      },
    })
  }

  const isEmpty = !isLoading && imports.length === 0

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Importar extrato
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Importe extratos bancários em OFX ou CSV
          </p>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 h-9 px-3 rounded-xl text-sm transition-colors"
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
          <RefreshCw size={15} />
          Atualizar
        </button>
      </div>

      {/* Upload */}
      <div
        className="p-6 rounded-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2">
            <Upload size={16} style={{ color: 'var(--ff-emerald)' }} />
            <h2 className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
              Novo arquivo
            </h2>
          </div>

          {/* Formato do arquivo */}
          <div
            role="tablist"
            aria-label="Formato do arquivo"
            className="inline-flex p-0.5 rounded-xl"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
          >
            {(['ofx', 'csv', 'paste'] as const).map(m => (
              <button
                key={m}
                type="button"
                role="tab"
                aria-selected={mode === m}
                onClick={() => selectMode(m)}
                className="h-8 px-4 rounded-[10px] text-xs font-semibold transition-colors"
                style={
                  mode === m
                    ? { background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald)' }
                    : { color: 'var(--ff-text-muted)' }
                }
              >
                {m === 'paste' ? tImports('paste.tab') : m.toUpperCase()}
              </button>
            ))}
          </div>
        </div>

        {mode === 'ofx' && (
          <OFXUploadZone
            onFileSelect={handleFileSelect}
            isLoading={isPending || uploading}
            initialFile={pastedFile}
          />
        )}
        {mode === 'csv' && <CsvImport onImported={openPreview} initialFile={pastedFile} />}
        {mode === 'paste' && (
          <PasteImport
            onReady={(format, file) => {
              setPastedFile(file)
              setMode(format)
            }}
          />
        )}
      </div>

      {/* Histórico */}
      <div>
        <h2 className="text-sm font-semibold mb-3" style={{ color: 'var(--ff-text-primary)' }}>
          Histórico de importações
        </h2>

        {isLoading && (
          <div className="flex items-center justify-center py-16">
            <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
          </div>
        )}

        {isEmpty && (
          <div className="flex flex-col items-center justify-center py-16 space-y-3">
            <div
              className="w-14 h-14 rounded-2xl flex items-center justify-center"
              style={{ background: 'var(--ff-bg-card)' }}
            >
              <FileText size={24} style={{ color: 'var(--ff-text-muted)' }} />
            </div>
            <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
              Nenhuma importação encontrada
            </p>
          </div>
        )}

        {!isLoading && !isEmpty && (
          <div className="space-y-2">
            {imports.map(item => (
              <ImportHistoryRow
                key={item.id}
                item={item}
                onPreview={id => navigate(`/imports/${id}/preview`)}
                onDelete={handleDelete}
                isDeleting={isDeleting && deletingId === item.id}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}