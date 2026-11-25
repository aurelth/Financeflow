import { useEffect, useMemo, useState } from 'react'
import { FileText, Loader2, Upload, X } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import AccountSelect from '@/features/accounts/components/AccountSelect'
import { useAccounts } from '@/features/accounts/api/useAccounts'
import { useLocale } from '@/hooks/useLocale'
import { getApiErrorMessage } from '@/lib/apiError'
import { useCsvAnalysis, useUploadCsv } from '../api/useImports'
import { CSV_DATE_FORMATS, type BankImportDto, type CsvMapping } from '../types/imports.types'

const MAX_BYTES = 5 * 1024 * 1024

const DELIMITERS: { value: string; key: string }[] = [
  { value: ';',  key: 'semicolon' },
  { value: ',',  key: 'comma' },
  { value: '\t', key: 'tab' },
  { value: '|',  key: 'pipe' },
]

const fieldStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  color:        'var(--ff-text-primary)',
  borderRadius: '8px',
  padding:      '8px 12px',
  fontSize:     '13px',
  outline:      'none',
}

const cardStyle: React.CSSProperties = {
  background: 'var(--ff-bg-card)',
  border:     '1px solid var(--ff-border)',
}

function sameMapping(a: CsvMapping, b: CsvMapping) {
  return JSON.stringify(a) === JSON.stringify(b)
}

// Mensagens do controller chegam como texto puro; as da aplicação, como { message, errors }
function errorText(err: unknown, fallback: string) {
  const data = (err as { response?: { data?: unknown } } | null)?.response?.data
  return typeof data === 'string' && data.length > 0 ? data : getApiErrorMessage(err, fallback)
}

interface FieldProps {
  id: string
  label: string
  children: React.ReactNode
}

function Field({ id, label, children }: FieldProps) {
  return (
    <div className="space-y-1.5 min-w-0">
      <label htmlFor={id} className="text-xs font-medium" style={{ color: 'var(--ff-text-secondary)' }}>
        {label}
      </label>
      {children}
    </div>
  )
}

interface Props {
  onImported: (result: BankImportDto) => void
  // Arquivo que já chega escolhido (ex.: texto colado na aba "Colar texto")
  initialFile?: File
}

export default function CsvImport({ onImported, initialFile }: Props) {
  const { t } = useTranslation('imports')
  const { t: tAccounts } = useTranslation('accounts')
  const { formatCurrency, formatDate } = useLocale()

  const [file, setFile]         = useState<File | null>(initialFile ?? null)
  const [fileError, setFileError] = useState<string | null>(null)
  const [dragging, setDragging]   = useState(false)
  // applied = mapeamento já enviado para análise (null = o que o back-end sugeriu na primeira leitura).
  // draft = edição em andamento; só vira applied depois de uma pausa, para não reenviar o arquivo a cada clique.
  const [applied, setApplied]     = useState<CsvMapping | null>(null)
  const [draft, setDraft]         = useState<CsvMapping | null>(null)
  const [accountId, setAccountId] = useState('')

  const { data: accounts = [] } = useAccounts()
  const { data, error, isFetching, isPlaceholderData } = useCsvAnalysis(file, applied)
  const { mutate: upload, isPending: isUploading } = useUploadCsv()

  // O back-end pode ter trocado o mapeamento (ex.: o separador mudou e as colunas antigas não servem mais):
  // a tela passa a seguir o que ele devolveu
  if (data && !isPlaceholderData && applied && !sameMapping(data.mapping, applied)) {
    setApplied(data.mapping)
    setDraft(null)
  }

  const mapping   = draft ?? data?.mapping ?? null
  const baseline  = applied ?? data?.mapping ?? null
  const pending   = !!mapping && !!baseline && !sameMapping(mapping, baseline)

  useEffect(() => {
    if (!draft || !baseline || sameMapping(draft, baseline)) return
    const id = setTimeout(() => setApplied(draft), 350)
    return () => clearTimeout(id)
  }, [draft, baseline])

  function update(patch: Partial<CsvMapping>) {
    if (mapping) setDraft({ ...mapping, ...patch })
  }

  function selectFile(next: File) {
    const name = next.name.toLowerCase()
    if (!name.endsWith('.csv') && !name.endsWith('.txt')) {
      setFileError(t('csv.onlyCsv'))
      return
    }
    if (next.size > MAX_BYTES) {
      setFileError(t('csv.tooBig'))
      return
    }
    setFileError(null)
    setApplied(null)
    setDraft(null)
    setFile(next)
  }

  function reset() {
    setFile(null)
    setDraft(null)
    setApplied(null)
    setFileError(null)
  }

  function handleImport() {
    if (!file || !baseline) return
    upload({ file, mapping: baseline, accountId: accountId || undefined }, {
      onSuccess: result => {
        toast.success(t('csv.uploaded'))
        if (result.skippedRows) toast.warning(t('csv.skipped', { count: result.skippedRows }))
        onImported(result)
      },
      onError: err => toast.error(errorText(err, t('csv.uploadError'))),
    })
  }

  const columnOptions = useMemo(() => {
    if (!data) return []
    return Array.from({ length: data.columns }, (_, i) => ({
      value: i,
      label: data.headers[i] ? `${i + 1} · ${data.headers[i]}` : `${t('csv.mapping.column')} ${i + 1}`,
    }))
  }, [data, t])

  // Papel de cada coluna, para marcar o cabeçalho da amostra
  const roles = useMemo(() => {
    const map = new Map<number, string[]>()
    if (!mapping) return map
    const add = (col: number | null, role: string) => {
      if (col === null) return
      map.set(col, [...(map.get(col) ?? []), t(`csv.roles.${role}`)])
    }
    add(mapping.dateColumn, 'date')
    add(mapping.descriptionColumn, 'description')
    add(mapping.amountColumn, 'amount')
    add(mapping.debitColumn, 'debit')
    add(mapping.creditColumn, 'credit')
    return map
  }, [mapping, t])

  // ── 1. Escolha do arquivo ───────────────────────────────────────────────────
  if (!file) {
    return (
      <div className="space-y-3">
        <div
          onDragOver={e => { e.preventDefault(); setDragging(true) }}
          onDragLeave={() => setDragging(false)}
          onDrop={e => {
            e.preventDefault()
            setDragging(false)
            const dropped = e.dataTransfer.files[0]
            if (dropped) selectFile(dropped)
          }}
          onClick={() => document.getElementById('csv-input')?.click()}
          className="border-2 border-dashed rounded-2xl p-10 flex flex-col items-center justify-center gap-3 transition-colors cursor-pointer"
          style={{
            borderColor: dragging ? 'var(--ff-emerald)' : 'var(--ff-border)',
            background:  dragging ? 'var(--ff-emerald-subtle)' : 'var(--ff-bg-card)',
          }}
        >
          <input
            id="csv-input"
            type="file"
            accept=".csv,.txt,text/csv"
            className="hidden"
            onChange={e => {
              const picked = e.target.files?.[0]
              if (picked) selectFile(picked)
              e.target.value = ''
            }}
          />
          <div className="w-12 h-12 rounded-xl flex items-center justify-center" style={{ background: 'var(--ff-bg-elevated)' }}>
            <Upload size={22} style={{ color: 'var(--ff-emerald)' }} />
          </div>
          <div className="text-center">
            <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>{t('csv.drag')}</p>
            <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>{t('csv.browse')}</p>
          </div>
        </div>
        <p className="text-xs px-1" style={{ color: 'var(--ff-text-muted)' }}>{t('csv.hint')}</p>
        {fileError && <p className="text-xs px-1" style={{ color: '#f87171' }}>{fileError}</p>}
      </div>
    )
  }

  const fileBar = (
    <div className="flex items-center justify-between gap-3 px-4 py-3 rounded-xl" style={cardStyle}>
      <div className="flex items-center gap-3 min-w-0">
        <FileText size={18} className="flex-shrink-0" style={{ color: 'var(--ff-emerald)' }} />
        <div className="min-w-0">
          <p className="text-sm font-medium break-all" style={{ color: 'var(--ff-text-primary)' }}>{file.name}</p>
          <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {(file.size / 1024).toFixed(1)} KB{data ? ` · ${data.encoding}` : ''}
          </p>
        </div>
      </div>
      <button
        type="button"
        onClick={reset}
        aria-label={t('csv.changeFile')}
        title={t('csv.changeFile')}
        className="p-1.5 rounded-lg transition-colors flex-shrink-0"
        style={{ color: 'var(--ff-text-muted)' }}
        onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
        onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
      >
        <X size={16} />
      </button>
    </div>
  )

  // ── 2. Lendo o arquivo / erro ───────────────────────────────────────────────
  if (!data || !mapping) {
    return (
      <div className="space-y-3">
        {fileBar}
        {error ? (
          <p role="alert" className="text-sm px-1" style={{ color: '#f87171' }}>
            {errorText(error, t('csv.analyzeError'))}
          </p>
        ) : (
          <div className="flex items-center gap-2 px-1 text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            <Loader2 size={16} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
            {t('csv.analyzing')}
          </div>
        )}
      </div>
    )
  }

  // ── 3. Mapeamento + resultado ───────────────────────────────────────────────
  const split = mapping.amountColumn === null
  const columnSelect = (id: string, value: number | null, onChange: (v: number | null) => void, optional = false) => (
    <select
      id={id}
      value={value ?? ''}
      onChange={e => onChange(e.target.value === '' ? null : Number(e.target.value))}
      style={fieldStyle}
    >
      {optional && <option value="">{t('csv.mapping.none')}</option>}
      {columnOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  )

  const canImport = data.validRows > 0 && !pending && !isFetching && !isUploading

  return (
    <div className="space-y-5">
      {fileBar}

      {/* Como ler as colunas */}
      <div className="space-y-3">
        <h3 className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>{t('csv.mapping.title')}</h3>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          <Field id="csv-delimiter" label={t('csv.mapping.delimiter')}>
            <select
              id="csv-delimiter"
              value={mapping.delimiter}
              onChange={e => update({ delimiter: e.target.value })}
              style={fieldStyle}
            >
              {DELIMITERS.map(d => <option key={d.key} value={d.value}>{t(`csv.delimiters.${d.key}`)}</option>)}
            </select>
          </Field>

          <Field id="csv-date-column" label={t('csv.mapping.dateColumn')}>
            {columnSelect('csv-date-column', mapping.dateColumn, v => update({ dateColumn: v ?? 0 }))}
          </Field>

          <Field id="csv-date-format" label={t('csv.mapping.dateFormat')}>
            <select
              id="csv-date-format"
              value={mapping.dateFormat}
              onChange={e => update({ dateFormat: e.target.value })}
              style={fieldStyle}
            >
              {CSV_DATE_FORMATS.map(f => <option key={f} value={f}>{f}</option>)}
            </select>
          </Field>

          <Field id="csv-description-column" label={t('csv.mapping.descriptionColumn')}>
            {columnSelect('csv-description-column', mapping.descriptionColumn, v => update({ descriptionColumn: v ?? 0 }))}
          </Field>

          <Field id="csv-amount-mode" label={t('csv.mapping.amountMode')}>
            <select
              id="csv-amount-mode"
              value={split ? 'split' : 'single'}
              onChange={e => {
                if (e.target.value === 'single')
                  update({ amountColumn: mapping.debitColumn ?? mapping.creditColumn ?? 0, debitColumn: null, creditColumn: null })
                else
                  update({ amountColumn: null, debitColumn: mapping.amountColumn, creditColumn: null, invertSign: false })
              }}
              style={fieldStyle}
            >
              <option value="single">{t('csv.mapping.single')}</option>
              <option value="split">{t('csv.mapping.split')}</option>
            </select>
          </Field>

          <Field id="csv-decimal" label={t('csv.mapping.decimal')}>
            <select
              id="csv-decimal"
              value={mapping.decimalSeparator}
              onChange={e => update({ decimalSeparator: e.target.value as ',' | '.' })}
              style={fieldStyle}
            >
              <option value=",">{t('csv.mapping.decimalComma')}</option>
              <option value=".">{t('csv.mapping.decimalDot')}</option>
            </select>
          </Field>

          {!split && (
            <Field id="csv-amount-column" label={t('csv.mapping.amountColumn')}>
              {columnSelect('csv-amount-column', mapping.amountColumn, v => update({ amountColumn: v ?? 0 }))}
            </Field>
          )}
          {split && (
            <>
              <Field id="csv-debit-column" label={t('csv.mapping.debitColumn')}>
                {columnSelect('csv-debit-column', mapping.debitColumn, v => update({ debitColumn: v }), true)}
              </Field>
              <Field id="csv-credit-column" label={t('csv.mapping.creditColumn')}>
                {columnSelect('csv-credit-column', mapping.creditColumn, v => update({ creditColumn: v }), true)}
              </Field>
            </>
          )}
        </div>

        <div className="flex flex-col gap-2">
          <label className="flex items-center gap-2 text-sm cursor-pointer" style={{ color: 'var(--ff-text-secondary)' }}>
            <input
              type="checkbox"
              checked={mapping.hasHeader}
              onChange={e => update({ hasHeader: e.target.checked })}
              className="w-4 h-4 accent-[var(--ff-emerald)]"
            />
            {t('csv.mapping.hasHeader')}
          </label>
          {!split && (
            <label className="flex items-center gap-2 text-sm cursor-pointer" style={{ color: 'var(--ff-text-secondary)' }}>
              <input
                type="checkbox"
                checked={mapping.invertSign}
                onChange={e => update({ invertSign: e.target.checked })}
                className="w-4 h-4 accent-[var(--ff-emerald)]"
              />
              {t('csv.mapping.invertSign')}
            </label>
          )}
        </div>
      </div>

      {/* Primeiras linhas do arquivo, com o papel de cada coluna */}
      <div className="space-y-2">
        <h3 className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>{t('csv.sample.title')}</h3>
        <div className="overflow-x-auto rounded-xl" style={cardStyle}>
          <table className="w-full text-xs">
            <thead>
              <tr style={{ borderBottom: '1px solid var(--ff-border)' }}>
                {Array.from({ length: data.columns }, (_, i) => (
                  <th key={i} className="text-left font-medium px-3 py-2 align-bottom whitespace-nowrap" style={{ color: 'var(--ff-text-secondary)' }}>
                    <div>{data.headers[i] || `${t('csv.mapping.column')} ${i + 1}`}</div>
                    <div className="flex gap-1 mt-1 min-h-[18px]">
                      {(roles.get(i) ?? []).map(r => (
                        <span
                          key={r}
                          className="px-1.5 py-0.5 rounded-full text-[10px] font-semibold"
                          style={{ background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald-text)' }}
                        >
                          {r}
                        </span>
                      ))}
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.sampleRows.map((row, r) => (
                <tr key={r} style={{ borderTop: r === 0 ? undefined : '1px solid var(--ff-border-subtle)' }}>
                  {row.map((cell, c) => (
                    <td key={c} className="px-3 py-1.5 whitespace-nowrap max-w-[16rem] truncate" style={{ color: 'var(--ff-text-muted)' }}>
                      {cell}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* O que a importação vai produzir */}
      <div className="space-y-2">
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
          <h3 className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>{t('csv.result.title')}</h3>
          <span className="text-xs" style={{ color: '#34d399' }}>{t('csv.result.ready', { count: data.validRows })}</span>
          {data.invalidRows > 0 && (
            <span className="text-xs" style={{ color: '#facc15' }}>{t('csv.result.ignored', { count: data.invalidRows })}</span>
          )}
          {(isFetching || pending) && (
            <Loader2 size={14} className="animate-spin" style={{ color: 'var(--ff-text-muted)' }} aria-label={t('csv.analyzing')} />
          )}
        </div>

        {data.validRows === 0 ? (
          <p role="status" className="text-sm px-1" style={{ color: '#f87171' }}>{t('csv.result.none')}</p>
        ) : (
          <div className="overflow-x-auto rounded-xl" style={cardStyle}>
            <table className="w-full text-xs">
              <thead>
                <tr style={{ borderBottom: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}>
                  <th className="text-left font-medium px-2 sm:px-3 py-2">{t('csv.result.date')}</th>
                  <th className="text-left font-medium px-2 sm:px-3 py-2">{t('csv.result.description')}</th>
                  <th className="text-right font-medium px-2 sm:px-3 py-2">{t('csv.result.amount')}</th>
                  <th className="text-left font-medium px-2 sm:px-3 py-2 max-sm:hidden">{t('csv.result.type')}</th>
                </tr>
              </thead>
              <tbody>
                {data.preview.map(row => {
                  const income = String(row.type) === 'Income'
                  return (
                    <tr key={row.line} style={{ borderTop: '1px solid var(--ff-border-subtle)' }}>
                      <td className="px-2 sm:px-3 py-1.5 whitespace-nowrap" style={{ color: 'var(--ff-text-secondary)' }}>
                        {formatDate(row.date)}
                      </td>
                      <td className="px-2 sm:px-3 py-1.5 max-w-[6.5rem] sm:max-w-[18rem] truncate" style={{ color: 'var(--ff-text-primary)' }}>
                        {row.description}
                      </td>
                      <td
                        className="px-2 sm:px-3 py-1.5 text-right whitespace-nowrap tabular-nums"
                        style={{ color: income ? '#34d399' : '#f87171' }}
                      >
                        {income ? '+' : '−'} {formatCurrency(Math.abs(row.amount))}
                      </td>
                      <td className="px-2 sm:px-3 py-1.5 whitespace-nowrap max-sm:hidden" style={{ color: 'var(--ff-text-muted)' }}>
                        {income ? t('csv.result.income') : t('csv.result.expense')}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {data.errors.length > 0 && (
          <details className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            <summary className="cursor-pointer select-none py-1" style={{ color: 'var(--ff-text-secondary)' }}>
              {t('csv.result.errorsTitle')}
            </summary>
            <ul className="mt-1 space-y-0.5 pl-4 list-disc">
              {data.errors.map(e => (
                <li key={`${e.line}-${e.code}`}>
                  {t('csv.result.line', { line: e.line })}: {t(`csv.errors.${e.code}`)}
                </li>
              ))}
              {data.invalidRows > data.errors.length && <li>…</li>}
            </ul>
          </details>
        )}
      </div>

      {/* Conta: CSV não identifica o banco, então a escolha é sempre do usuário */}
      {accounts.length > 1 && (
        <div className="space-y-1.5">
          <label htmlFor="csv-account" className="text-xs font-medium" style={{ color: 'var(--ff-text-secondary)' }}>
            {tAccounts('import.question')}
          </label>
          <AccountSelect id="csv-account" value={accountId} onChange={setAccountId} emptyOption="default" />
          <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>{t('csv.accountHint')}</p>
        </div>
      )}

      <div className="flex justify-end gap-2">
        <button
          type="button"
          onClick={reset}
          className="h-9 px-4 rounded-xl text-sm transition-colors"
          style={{ color: 'var(--ff-text-secondary)', background: 'var(--ff-bg-elevated)' }}
        >
          {t('csv.cancel')}
        </button>
        <button
          type="button"
          onClick={handleImport}
          disabled={!canImport}
          className="flex items-center gap-2 h-9 px-5 rounded-xl text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
        >
          {isUploading && <Loader2 size={14} className="animate-spin" />}
          {isUploading ? t('csv.importing') : t('csv.import')}
        </button>
      </div>
    </div>
  )
}
