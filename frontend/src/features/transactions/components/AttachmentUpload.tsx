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

export default function AttachmentUpload({
  transactionId,
  attachmentPath,
}: AttachmentUploadProps) {  
  const uploadRef    = useRef<HTMLInputElement>(null)
  const upload       = useUploadAttachment(transactionId)

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) upload.mutate(file)
  }

  return (
    <div className="space-y-2">
      <label className="text-xs font-medium text-slate-400 uppercase tracking-wide">
        Anexo
      </label>

      {attachmentPath ? (
        <div className="flex items-center gap-3 bg-slate-800 border border-slate-700 rounded-xl px-3 py-2.5">
          <div className="w-8 h-8 rounded-lg bg-slate-700 flex items-center justify-center flex-shrink-0">
            {isImage(attachmentPath)
              ? <Image size={16} className="text-indigo-400" />
              : <FileText size={16} className="text-indigo-400" />
            }
          </div>
          <span className="text-sm text-slate-300 truncate flex-1">
            {getFileName(attachmentPath)}
          </span>
          <button
            onClick={() => uploadRef.current?.click()}
            className="text-xs text-indigo-400 hover:text-indigo-300 transition-colors flex-shrink-0"
          >
            Substituir
          </button>
        </div>
      ) : (
        <button
          onClick={() => uploadRef.current?.click()}
          disabled={upload.isPending}
          className="w-full flex items-center justify-center gap-2 border border-dashed border-slate-700 rounded-xl py-3 text-sm text-slate-500 hover:text-slate-300 hover:border-slate-600 transition-colors disabled:opacity-50"
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