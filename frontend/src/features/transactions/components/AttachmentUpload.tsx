import { useRef } from 'react'
import { Paperclip, FileText, Image } from 'lucide-react'
import { useUploadAttachment } from '../api/useTransactions'

interface AttachmentUploadProps {
  transactionId:  string
  attachmentPath: string | null
}

function getFileName(path: string) {
  return path.split('/').pop() ?? path
}

function isImage(path: string) {
  return /\.(jpg|jpeg|png|webp)$/i.test(path)
}

export default function AttachmentUpload({ transactionId, attachmentPath }: AttachmentUploadProps) {
  const uploadRef = useRef<HTMLInputElement>(null)
  const upload    = useUploadAttachment(transactionId)

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) upload.mutate(file)
  }

  return (
    <div className="space-y-2">
      <label
        className="text-xs font-medium uppercase tracking-wide"
        style={{ color: 'var(--ff-text-muted)' }}
      >
        Anexo
      </label>

      {attachmentPath ? (
        <div
          className="flex items-center gap-3 rounded-xl px-3 py-2.5"
          style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
        >
          <div
            className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
            style={{ background: 'var(--ff-bg-card)' }}
          >
            {isImage(attachmentPath)
              ? <Image size={16} style={{ color: 'var(--ff-emerald)' }} />
              : <FileText size={16} style={{ color: 'var(--ff-emerald)' }} />
            }
          </div>
          <span
            className="text-sm truncate flex-1"
            style={{ color: 'var(--ff-text-secondary)' }}
          >
            {getFileName(attachmentPath)}
          </span>
          <button
            onClick={() => uploadRef.current?.click()}
            className="text-xs flex-shrink-0 transition-colors"
            style={{ color: 'var(--ff-emerald)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald-hover)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
          >
            Substituir
          </button>
        </div>
      ) : (
        <button
          onClick={() => uploadRef.current?.click()}
          disabled={upload.isPending}
          className="w-full flex items-center justify-center gap-2 rounded-xl py-3 text-sm transition-colors disabled:opacity-50"
          style={{ border: '1px dashed var(--ff-border)', color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => {
            e.currentTarget.style.color = 'var(--ff-text-secondary)'
            e.currentTarget.style.borderColor = 'var(--ff-emerald)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.color = 'var(--ff-text-muted)'
            e.currentTarget.style.borderColor = 'var(--ff-border)'
          }}
        >
          <Paperclip size={14} />
          {upload.isPending ? 'Enviando...' : 'Adicionar anexo'}
        </button>
      )}

      <input
        ref={uploadRef}
        type="file"
        accept="image/jpeg,image/png,image/webp,application/pdf"
        onChange={handleChange}
        className="hidden"
      />
    </div>
  )
}