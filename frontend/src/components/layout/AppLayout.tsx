import { Outlet } from 'react-router-dom'
import Sidebar          from './Sidebar'
import Header           from './Header'
import ErrorBoundary    from '@/components/ErrorBoundary'
import { useReportHub }       from '@/features/reports/hooks/useReportHub'
import { useNotificationHub } from '@/hooks/useNotificationHub'

export default function AppLayout() {
  useReportHub()
  useNotificationHub()

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: 'var(--ff-bg-base)' }}>
      {/* Sidebar — desktop sempre visível, mobile via drawer (gerido internamente) */}
      <Sidebar />
      <div className="flex-1 flex flex-col overflow-hidden min-w-0"> {/* min-w-0 */}
        <Header />
        <main className="flex-1 overflow-y-auto p-4 md:p-6" style={{ background: 'var(--ff-bg-base)' }}> {/* p responsivo */}
          <ErrorBoundary>
            <Outlet />
          </ErrorBoundary>
        </main>
      </div>
    </div>
  )
}