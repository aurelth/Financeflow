import type { ReportByCategoryDto } from '../../types/analytics.types'
export default function ByCategoryTab({ data }: { data: ReportByCategoryDto }) {
  return <div>Por Categoria — em desenvolvimento ({data.categories.length} categorias)</div>
}