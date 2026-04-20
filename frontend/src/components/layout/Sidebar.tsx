import { NavLink, useLocation } from 'react-router-dom'
import { useEffect } from 'react'
import {
  LayoutDashboard, ArrowLeftRight, Tag, PiggyBank,
  BarChart3, GitCompare, FileText, Settings, ShieldCheck, Upload, X, TrendingUp, Sparkles, HeartPulse,
} from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { cn } from '@/lib/utils'
import { useAuthStore }    from '@/store/authStore'
import { useTranslation }  from 'react-i18next'
import { useSidebarStore } from '@/store/sidebarStore'

const navLinkStyle = ({ isActive }: { isActive: boolean }) =>
  isActive
    ? { background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald)' }
    : { color: 'var(--ff-text-muted)' }

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200',
    isActive ? 'ff-nav-active' : 'ff-nav-default'
  )

function onNavEnter(e: React.MouseEvent<HTMLAnchorElement>) {
  const el = e.currentTarget
  if (!el.classList.contains('ff-nav-active')) {
    el.style.color      = 'var(--ff-text-secondary)'
    el.style.background = 'var(--ff-bg-elevated)'
  }
}

function onNavLeave(e: React.MouseEvent<HTMLAnchorElement>) {
  const el = e.currentTarget
  if (!el.classList.contains('ff-nav-active')) {
    el.style.color      = 'var(--ff-text-muted)'
    el.style.background = 'transparent'
  }
}

export default function Sidebar() {
  const { t }      = useTranslation('common')
  const user       = useAuthStore(s => s.user)
  const isAdmin    = user?.role === 'Admin'
  const location   = useLocation()
  const { isOpen, close } = useSidebarStore()

  // Fecha sidebar ao navegar em mobile
  useEffect(() => {
    close()
  }, [location.pathname])

  const navItems = [
    { to: '/dashboard',    icon: LayoutDashboard, label: t('nav.dashboard')    },
    { to: '/transactions', icon: ArrowLeftRight,  label: t('nav.transactions') },
    { to: '/categories',   icon: Tag,             label: t('nav.categories')   },
    { to: '/budgets',      icon: PiggyBank,       label: t('nav.budgets')      },
    { to: '/comparison',   icon: GitCompare,      label: t('nav.comparison')   },
    { to: '/reports',      icon: BarChart3,       label: t('nav.reports')      },
    { to: '/exports',      icon: FileText,        label: t('nav.exports')      },
    { to: '/imports',      icon: Upload,          label: t('nav.imports')      },
    { to: '/health-score', icon: HeartPulse,      label: t('nav.healthScore')  },
  ]

  const sidebarContent = (
    <aside
      className="w-64 flex flex-col h-full"
      style={{ background: 'var(--ff-bg-card)', borderRight: '1px solid var(--ff-border)' }}
    >
      {/* Logo */}
      <div className="p-6 flex items-center justify-between" style={{ borderBottom: '1px solid var(--ff-border)' }}>
        <div className="flex items-center gap-3">
          <div
            className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0"
            style={{ background: 'var(--ff-emerald)' }}
          >
            <TrendingUp size={18} style={{ color: 'var(--ff-emerald-subtle)' }} />
          </div>
          <div>
            <p style={{ color: 'var(--ff-text-primary)', fontWeight: 600, fontSize: 14 }}>FinanceFlow</p>
            <p style={{ color: 'var(--ff-text-muted)', fontSize: 11 }}>Gestão Financeira</p>
          </div>
        </div>
        {/* Botão fechar em mobile */}
        <button
          onClick={close}
          className="lg:hidden p-1.5 rounded-lg transition-colors"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
        >
          <X size={18} />
        </button>
      </div>

      {/* Navegação principal */}
      <nav className="flex-1 p-4 space-y-0.5 overflow-y-auto">
        {navItems.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            className={navLinkClass}
            style={navLinkStyle}
            onMouseEnter={onNavEnter}
            onMouseLeave={onNavLeave}
          >
            <Icon size={18} />
            {label}
          </NavLink>
        ))}

        {/* Divisor e link do Assistente IA */}
        <div
          className="my-2"
          style={{ borderTop: '1px solid var(--ff-border)', marginLeft: '-4px', marginRight: '-4px' }}
        />
        <NavLink
          to="/assistant"
          className={navLinkClass}
          style={navLinkStyle}
          onMouseEnter={onNavEnter}
          onMouseLeave={onNavLeave}
        >
          <Sparkles size={18} />
          {t('nav.assistant')}
        </NavLink>

        {isAdmin && (
          <>
            <div
              className="my-2"
              style={{ borderTop: '1px solid var(--ff-border)', marginLeft: '-4px', marginRight: '-4px' }}
            />
            <NavLink
              to="/admin"
              className={navLinkClass}
              style={navLinkStyle}
              onMouseEnter={onNavEnter}
              onMouseLeave={onNavLeave}
            >
              <ShieldCheck size={18} />
              {t('nav.admin')}
            </NavLink>
          </>
        )}
      </nav>

      {/* Configurações */}
      <div className="p-4" style={{ borderTop: '1px solid var(--ff-border)' }}>
        <NavLink
          to="/settings"
          className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200"
          style={({ isActive }) =>
            isActive
              ? { background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald)' }
              : { color: 'var(--ff-text-muted)' }
          }
          onMouseEnter={e => {
            e.currentTarget.style.color      = 'var(--ff-text-secondary)'
            e.currentTarget.style.background = 'var(--ff-bg-elevated)'
          }}
          onMouseLeave={e => {
            if (window.location.pathname !== '/settings') {
              e.currentTarget.style.color      = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }
          }}
        >
          <Settings size={18} />
          {t('nav.settings')}
        </NavLink>
      </div>
    </aside>
  )

  return (
    <>
      {/* Desktop — sidebar sempre visível */}
      <div className="hidden lg:flex">
        {sidebarContent}
      </div>

      {/* Mobile — sidebar com overlay animado */}
      <AnimatePresence>
        {isOpen && (
          <>
            <motion.div
              key="overlay"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.2 }}
              className="fixed inset-0 z-40 lg:hidden"
              style={{ background: 'rgba(0,0,0,0.6)' }}
              onClick={close}
            />
            <motion.div
              key="drawer"
              initial={{ x: '-100%' }}
              animate={{ x: 0 }}
              exit={{ x: '-100%' }}
              transition={{ type: 'spring', damping: 25, stiffness: 300 }}
              className="fixed inset-y-0 left-0 z-50 lg:hidden"
            >
              {sidebarContent}
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </>
  )
}