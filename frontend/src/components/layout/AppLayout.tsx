import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import Header from './Header'
import { useReportHub } from '@/features/reports/hooks/useReportHub'
import { useNotificationHub } from '@/hooks/useNotificationHub'

export default function AppLayout() {
  useReportHub()
  useNotificationHub()

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: 'var(--ff-bg-base)' }}> {/* Modificado */}
      <Sidebar />
      <div className="flex-1 flex flex-col overflow-hidden">
        <Header />
        <main className="flex-1 overflow-y-auto p-6" style={{ background: 'var(--ff-bg-base)' }}> {/* Modificado */}
          <Outlet />
        </main>
      </div>
    </div>
  )
}