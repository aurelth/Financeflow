import type { AnnualSummaryDto } from '../../types/analytics.types'
export default function AnnualSummaryTab({ data }: { data: AnnualSummaryDto }) {
  return <div>Resumo Anual — em desenvolvimento ({data.year})</div>
}