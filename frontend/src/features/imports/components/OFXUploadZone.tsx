import { useCallback, useState } from 'react'
import { Upload, FileText, X } from 'lucide-react'

interface Props {
  onFileSelect: (file: File) => void
  isLoading: boolean
}

export default function OFXUploadZone({ onFileSelect, isLoading }: Props) {
  const [dragging, setDragging]     = useState(false)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [error, setError]           = useState<string | null>(null)

  function validateAndSelect(file: File) {
    if (!file.name.toLowerCase().endsWith('.ofx')) {
      setError('Apenas ficheiros .ofx são suportados.')
      setSelectedFile(null)
      return
    }
    setError(null)
    setSelectedFile(file)
  }

  const onDrop = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    setDragging(false)
    const file = e.dataTransfer.files[0]
    if (file) validateAndSelect(file)
  }, [])

  function onInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) validateAndSelect(file)
  }

  function handleUpload() {
    if (selectedFile) onFileSelect(selectedFile)
  }

  function handleClear() {
    setSelectedFile(null)
    setError(null)
  }

  return (
    <div className="space-y-4">
      {/* Zona de drop */}
      <div
        onDragOver={e => { e.preventDefault(); setDragging(true) }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        className="relative border-2 border-dashed rounded-2xl p-10 flex flex-col items-center justify-center gap-3 transition-colors cursor-pointer"
        style={{
          borderColor: dragging ? 'var(--ff-emerald)' : 'var(--ff-border)',
          background:  dragging ? 'var(--ff-emerald-subtle)' : 'var(--ff-bg-card)',
        }}
        onClick={() => document.getElementById('ofx-input')?.click()}
      >
        <input
          id="ofx-input"
          type="file"
          accept=".ofx"
          className="hidden"
          onChange={onInputChange}
        />

        <div
          className="w-12 h-12 rounded-xl flex items-center justify-center"
          style={{ background: 'var(--ff-bg-elevated)' }}
        >
          <Upload size={22} style={{ color: 'var(--ff-emerald)' }} />
        </div>

        <div className="text-center">
          <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            Arraste o ficheiro OFX aqui
          </p>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            ou clique para selecionar
          </p>
        </div>
      </div>

      {/* Erro */}
      {error && (
        <p className="text-xs px-1" style={{ color: '#f87171' }}>{error}</p>
      )}

      {/* Ficheiro selecionado */}
      {selectedFile && !error && (
        <div
          className="flex items-center justify-between px-4 py-3 rounded-xl"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <div className="flex items-center gap-3">
            <FileText size={18} style={{ color: 'var(--ff-emerald)' }} />
            <div>
              <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
                {selectedFile.name}
              </p>
              <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                {(selectedFile.size / 1024).toFixed(1)} KB
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={handleClear}
              className="p-1.5 rounded-lg transition-colors"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
            >
              <X size={15} />
            </button>
            <button
              onClick={handleUpload}
              disabled={isLoading}
              className="flex items-center gap-2 h-8 px-4 rounded-xl text-xs font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
              onMouseEnter={e => {
                if (!isLoading) e.currentTarget.style.background = 'var(--ff-emerald-hover)'
              }}
              onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
            >
              {isLoading ? 'A enviar...' : 'Enviar'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}