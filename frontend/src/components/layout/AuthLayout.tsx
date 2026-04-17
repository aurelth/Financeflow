import { Outlet } from 'react-router-dom'

export default function AuthLayout() {
  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 relative"
      style={{ background: 'var(--ff-bg-base)' }}
    >
      {/* Decoração sutil com verde esmeralda */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div
          className="absolute -top-40 -right-40 w-96 h-96 rounded-full blur-3xl"
          style={{ background: 'rgba(16, 185, 129, 0.06)' }}
        />
        <div
          className="absolute -bottom-40 -left-40 w-96 h-96 rounded-full blur-3xl"
          style={{ background: 'rgba(16, 185, 129, 0.04)' }}
        />
      </div>

      {/* Logo */}
      <div className="absolute top-8 left-8 flex items-center gap-2">
        <div
          className="w-8 h-8 rounded-lg flex items-center justify-center"
          style={{ background: 'var(--ff-emerald)' }}
        >
          <span style={{ color: 'var(--ff-emerald-subtle)', fontWeight: 700, fontSize: 13 }}>FF</span>
        </div>
        <span style={{ color: 'var(--ff-text-primary)', fontWeight: 600, fontSize: 17 }}>FinanceFlow</span>
      </div>

      <div className="relative w-full max-w-md">
        <Outlet />
      </div>
    </div>
  )
}