import type { CashFlowDto } from '../../types/analytics.types'
export default function CashFlowTab({ data }: { data: CashFlowDto }) {
  return <div>Fluxo de Caixa — em desenvolvimento ({data.periods.length} períodos)</div>
}