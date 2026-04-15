import type { ProjectionsDto } from '../../types/analytics.types'
export default function ProjectionsTab({ data }: { data: ProjectionsDto }) {
  return <div>Projecções — em desenvolvimento ({data.projected.length} meses projectados)</div>
}