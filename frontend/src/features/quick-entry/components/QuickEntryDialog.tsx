import { useRef, useState } from 'react'
import { Camera, Loader2, Sparkles, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '@/lib/apiError'
import { useAiQuota, useParseQuickEntry } from '../api/useQuickEntry'
import { downscaleImage } from '../lib/downscaleImage'
import type { QuickEntryBatch } from '../types/quickEntry.types'

// Espelham Ai:MaxTextLength e Ai:MaxImageBytes do back-end (o servidor é quem manda; aqui é só conforto)
export const MAX_TEXT_LENGTH = 500
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024

interface QuickEntryDialogProps {
  onClose:  () => void
  onResult: (batch: QuickEntryBatch) => void
}

export default function QuickEntryDialog({ onClose, onResult }: QuickEntryDialogProps) {
  const { t } = useTranslation('transactions')

  const [text, setText]           = useState('')
  const [image, setImage]         = useState<File | null>(null)
  const [error, setError]         = useState<string | null>(null)
  const [preparing, setPreparing] = useState(false)
  const [nothingFound, setNothingFound] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const { data: quota } = useAiQuota()
  const parse = useParseQuickEntry()

  const exhausted = !!quota && quota.remaining <= 0
  const hasInput  = text.trim().length > 0 || !!image
  const busy      = parse.isPending || preparing
  const canSubmit = hasInput && !busy && !exhausted

  async function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const picked = e.target.files?.[0]
    e.target.value = ''
    if (!picked) return

    setError(null)
    setNothingFound(false)
    setPreparing(true)
    try {
      const prepared = await downscaleImage(picked)
      if (prepared.size > MAX_IMAGE_BYTES) {
        setImage(null)
        setError(t('quickEntry.imageTooLarge'))
        return
      }
      setImage(prepared)
    } finally {
      setPreparing(false)
    }
  }

  function handleSubmit() {
    if (!canSubmit) return
    setError(null)
    setNothingFound(false)

    parse.mutate({ text, image }, {
      onSuccess: result => {
        if (result.drafts.length === 0) { setNothingFound(true); return }
        onResult({ drafts: result.drafts, image })
      },
      onError: err => setError(getApiErrorMessage(err, t('quickEntry.genericError'))),
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="quick-entry-title"
        className="w-full max-w-lg rounded-2xl shadow-2xl max-h-[calc(100dvh-2rem)] overflow-y-auto"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="flex items-center justify-between px-6 py-4"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <h2 id="quick-entry-title" className="flex items-center gap-2 font-semibold text-lg" style={{ color: 'var(--ff-text-primary)' }}>
            <Sparkles size={18} style={{ color: 'var(--ff-emerald)' }} />
            {t('quickEntry.title')}
          </h2>
          <button
            onClick={onClose}
            aria-label={t('quickEntry.close')}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            <X size={18} />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            {t('quickEntry.subtitle')}
          </p>

          <div>
            <textarea
              value={text}
              onChange={e => { setText(e.target.value); setNothingFound(false) }}
              maxLength={MAX_TEXT_LENGTH}
              rows={3}
              aria-label={t('quickEntry.textLabel')}
              placeholder={t('quickEntry.textPlaceholder')}
              className="w-full resize-none"
              style={{
                background: 'var(--ff-bg-elevated)',
                border: '1px solid var(--ff-border)',
                borderRadius: '12px',
                padding: '10px 16px',
                color: 'var(--ff-text-primary)',
                fontSize: '14px',
                outline: 'none',
              }}
            />
            <p className="text-xs mt-1 text-right" style={{ color: 'var(--ff-text-muted)' }}>
              {text.length}/{MAX_TEXT_LENGTH}
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => fileRef.current?.click()}
              disabled={busy}
              className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium disabled:opacity-60"
              style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-primary)' }}
            >
              <Camera size={15} />
              {image ? t('quickEntry.changePhoto') : t('quickEntry.addPhoto')}
            </button>
            {image && (
              <span className="flex items-center gap-1.5 text-xs min-w-0" style={{ color: 'var(--ff-text-muted)' }}>
                <span className="truncate" data-testid="quick-entry-image-name">{image.name}</span>
                <button
                  type="button"
                  onClick={() => setImage(null)}
                  aria-label={t('quickEntry.removePhoto')}
                  className="p-0.5 rounded"
                >
                  <X size={12} />
                </button>
              </span>
            )}
            <input
              ref={fileRef}
              type="file"
              accept="image/*"
              data-testid="quick-entry-file"
              className="hidden"
              onChange={handleFile}
            />
          </div>

          {error && (
            <p role="alert" className="text-sm rounded-xl px-3 py-2"
              style={{ background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.3)', color: 'var(--ff-expense)' }}>
              {error}
            </p>
          )}

          {nothingFound && (
            <p role="status" className="text-sm rounded-xl px-3 py-2"
              style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}>
              {t('quickEntry.nothingFound')}
            </p>
          )}

          {quota && (
            <p className="text-xs" data-testid="quick-entry-quota"
              style={{ color: exhausted ? 'var(--ff-expense)' : 'var(--ff-text-muted)' }}>
              {exhausted
                ? t('quickEntry.quotaExhausted', { limit: quota.limit })
                : t('quickEntry.quotaRemaining', { remaining: quota.remaining, limit: quota.limit })}
            </p>
          )}
        </div>

        <div className="flex gap-3 px-6 py-4" style={{ borderTop: '1px solid var(--ff-border)' }}>
          <button
            onClick={onClose}
            className="flex-1 h-10 rounded-xl text-sm font-medium"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}
          >
            {t('quickEntry.cancel')}
          </button>
          <button
            onClick={handleSubmit}
            disabled={!canSubmit}
            className="flex-1 h-10 rounded-xl text-sm font-medium flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          >
            {busy && <Loader2 size={15} className="animate-spin" />}
            {busy ? t('quickEntry.reading') : t('quickEntry.submit')}
          </button>
        </div>
      </div>
    </div>
  )
}
