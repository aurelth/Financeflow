import { useMemo, useState } from 'react'
import { ClipboardPaste, Eraser } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import {
  MAX_PASTED_BYTES, byteLength, detectFormat, normalizePasted, toStatementFile, type PastedFormat,
} from '../lib/pastedStatement'

interface Props {
  // Entrega o texto como arquivo ao fluxo do formato detectado (OFX ou CSV)
  onReady: (format: Exclude<PastedFormat, 'unknown'>, file: File) => void
}

export default function PasteImport({ onReady }: Props) {
  const { t } = useTranslation('imports')
  const [text, setText] = useState('')

  const normalized = useMemo(() => normalizePasted(text), [text])
  const format     = useMemo(() => detectFormat(normalized), [normalized])
  const tooBig     = useMemo(() => byteLength(normalized) > MAX_PASTED_BYTES, [normalized])
  const canPaste   = typeof navigator !== 'undefined' && !!navigator.clipboard?.readText

  async function pasteFromClipboard() {
    try {
      setText(await navigator.clipboard.readText())
    } catch {
      // Permissão negada: o usuário ainda pode colar direto na caixa
      toast.error(t('paste.clipboardDenied'))
    }
  }

  const hasText = normalized.length > 0
  const canContinue = hasText && format !== 'unknown' && !tooBig

  let status: { message: string; color: string } | null = null
  if (hasText) {
    if (tooBig)                  status = { message: t('paste.tooBig'), color: '#f87171' }
    else if (format === 'unknown') status = { message: t('paste.unknown'), color: '#facc15' }
    else                          status = { message: t('paste.detected', { format: format.toUpperCase() }), color: '#34d399' }
  }

  return (
    <div className="space-y-3">
      <div className="space-y-1.5">
        <label htmlFor="paste-text" className="text-xs font-medium" style={{ color: 'var(--ff-text-secondary)' }}>
          {t('paste.label')}
        </label>
        <textarea
          id="paste-text"
          value={text}
          onChange={e => setText(e.target.value)}
          rows={10}
          spellCheck={false}
          autoCapitalize="off"
          autoCorrect="off"
          placeholder={t('paste.placeholder')}
          aria-describedby="paste-hint"
          className="w-full rounded-xl p-3 font-mono text-base md:text-xs resize-y min-h-[10rem] max-h-[28rem] outline-none focus-visible:ring-2 focus-visible:ring-[var(--ff-emerald)]"
          style={{
            background: 'var(--ff-bg-elevated)',
            border:     '1px solid var(--ff-border)',
            color:      'var(--ff-text-primary)',
          }}
        />
        <p id="paste-hint" className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>{t('paste.tip')}</p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {canPaste && (
          <button
            type="button"
            onClick={pasteFromClipboard}
            className="flex items-center gap-2 h-9 px-3 rounded-xl text-sm transition-colors"
            style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
          >
            <ClipboardPaste size={15} />
            {t('paste.fromClipboard')}
          </button>
        )}
        {hasText && (
          <button
            type="button"
            onClick={() => setText('')}
            className="flex items-center gap-2 h-9 px-3 rounded-xl text-sm transition-colors"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            <Eraser size={15} />
            {t('paste.clear')}
          </button>
        )}
        <div className="flex-1" />
        {status && (
          <span role="status" className="text-xs" style={{ color: status.color }}>{status.message}</span>
        )}
        <button
          type="button"
          disabled={!canContinue}
          onClick={() => format !== 'unknown' && onReady(format, toStatementFile(normalized, format))}
          className="h-9 px-5 rounded-xl text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
        >
          {t('paste.continue')}
        </button>
      </div>
    </div>
  )
}
