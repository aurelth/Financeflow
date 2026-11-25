// Texto colado de um extrato (OFX ou CSV) → arquivo em memória.
// Assim o texto colado entra pelos mesmos endpoints e validações de quem envia arquivo:
// nada de caminho paralelo no servidor.

export type PastedFormat = 'ofx' | 'csv' | 'unknown'

// Mesmo limite do upload de CSV no servidor
export const MAX_PASTED_BYTES = 5 * 1024 * 1024

const CSV_DELIMITERS = [';', ',', '\t', '|']

function looksLikeOfx(text: string) {
  return /OFXHEADER|<OFX[\s>]/i.test(text)
}

/**
 * Limpa o que costuma vir junto quando o texto é copiado de uma conversa com IA:
 * cerca de código (```), texto antes e depois dela, BOM e recuo nas linhas.
 */
export function normalizePasted(raw: string): string {
  let text = raw.replace(/^\uFEFF/, '')

  const fenced = /```[^\n]*\n([\s\S]*?)```/.exec(text)
  if (fenced) {
    text = fenced[1]
  } else {
    // Cerca aberta e nunca fechada (resposta cortada na cópia)
    text = text.replace(/^\s*```[^\n]*\n/, '').replace(/\n```\s*$/, '')
  }

  text = text.trim()

  // No OFX o recuo não significa nada; o cabeçalho, porém, precisa começar na coluna 0
  return looksLikeOfx(text)
    ? text.split(/\r?\n/).map(line => line.trimStart()).join('\n')
    : text
}

export function detectFormat(text: string): PastedFormat {
  if (looksLikeOfx(text)) return 'ofx'

  // CSV: ao menos cabeçalho + uma linha, todas com o mesmo separador
  const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0).slice(0, 5)
  if (lines.length < 2) return 'unknown'

  return CSV_DELIMITERS.some(d => lines.every(l => l.includes(d))) ? 'csv' : 'unknown'
}

export function byteLength(text: string): number {
  return new TextEncoder().encode(text).length
}

// yyyyMMdd-HHmm no fuso local: identifica a importação no histórico
function stamp(now: Date) {
  const p = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}${p(now.getMonth() + 1)}${p(now.getDate())}-${p(now.getHours())}${p(now.getMinutes())}`
}

export function toStatementFile(text: string, format: Exclude<PastedFormat, 'unknown'>, now = new Date()): File {
  const type = format === 'ofx' ? 'application/x-ofx' : 'text/csv'
  return new File([text], `colado-${stamp(now)}.${format}`, { type })
}
