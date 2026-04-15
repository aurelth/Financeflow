import type { ReportByTagDto } from '../../types/analytics.types'
export default function ByTagTab({ data }: { data: ReportByTagDto }) {
  return <div>Por Tag — em desenvolvimento ({data.tags.length} tags)</div>
}