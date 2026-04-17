import { Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence }     from 'framer-motion'
import Sidebar                 from './Sidebar'
import Header                  from './Header'
import ErrorBoundary           from '@/components/ErrorBoundary'
import PageTransition          from '@/components/PageTransition'
import { useScrollToTop }      from '@/hooks/useScrollToTop'
import { useReportHub }        from '@/features/reports/hooks/useReportHub'
import { useNotificationHub }  from '@/hooks/useNotificationHub'

export default function AppLayout() {
  useReportHub()
  useNotificationHub()
  useScrollToTop()

  const location = useLocation()

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: 'var(--ff-bg-base)' }}>
      <Sidebar />
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        <Header />
        <main className="flex-1 overflow-y-auto p-4 md:p-6" style={{ background: 'var(--ff-bg-base)' }}>
          <ErrorBoundary>
            {/* Transições entre páginas */}
            <AnimatePresence mode="wait">
              <PageTransition key={location.pathname}>
                <Outlet />
              </PageTransition>
            </AnimatePresence>
          </ErrorBoundary>
        </main>
      </div>
    </div>
  )
}