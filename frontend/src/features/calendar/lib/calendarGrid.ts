// Datas do calendário trafegam como "yyyy-MM-dd": comparar texto evita o deslocamento de fuso
// que `new Date('2026-11-24')` (UTC) causaria ao ser exibido em hora local.

export function toDateKey(year: number, month: number, day: number): string {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

/** "2026-11-24T00:00:00" → "2026-11-24" */
export function dateKeyOf(isoDate: string): string {
  return isoDate.slice(0, 10)
}

export interface GridCell {
  day: number
  key: string
}

/**
 * Semanas do mês, de domingo a sábado. As posições antes do dia 1 e depois do último dia vêm `null`,
 * de modo que toda semana tem exatamente 7 posições.
 */
export function buildMonthGrid(year: number, month: number): (GridCell | null)[][] {
  const firstWeekday = new Date(year, month - 1, 1).getDay()
  const daysInMonth  = new Date(year, month, 0).getDate()

  const cells: (GridCell | null)[] = Array.from({ length: firstWeekday }, () => null)
  for (let day = 1; day <= daysInMonth; day++) {
    cells.push({ day, key: toDateKey(year, month, day) })
  }
  while (cells.length % 7 !== 0) cells.push(null)

  const weeks: (GridCell | null)[][] = []
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7))
  return weeks
}

export function shiftMonth(year: number, month: number, delta: number): { year: number; month: number } {
  const index = year * 12 + (month - 1) + delta
  return { year: Math.floor(index / 12), month: (index % 12) + 1 }
}
