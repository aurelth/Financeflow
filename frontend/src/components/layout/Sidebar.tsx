import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  ArrowLeftRight,
  Tag,
  PiggyBank,
  BarChart3,
  GitCompare,
  FileText,
  Settings,
  ShieldCheck,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/authStore'

const navItems = [
  { to: '/dashboard',    icon: LayoutDashboard, label: 'Dashboard'   },
  { to: '/transactions', icon: ArrowLeftRight,  label: 'Transações'  },
  { to: '/categories',   icon: Tag,             label: 'Categorias'  },
  { to: '/budgets',      icon: PiggyBank,       label: 'Orçamentos'  },
  { to: '/comparison',   icon: GitCompare,      label: 'Comparativo' },
  { to: '/reports',      icon: BarChart3,       label: 'Relatórios'  },
  { to: '/exports',      icon: FileText,        label: 'Exportar'    },
]

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
  const user    = useAuthStore(s => s.user)
  const isAdmin = user?.role === 'Admin'

  return (
    <aside
      className="w-64 flex flex-col"
      style={{ background: 'var(--ff-bg-card)', borderRight: '1px solid var(--ff-border)' }}
    >
      {/* Logo */}
      <div className="p-6" style={{ borderBottom: '1px solid var(--ff-border)' }}>
        <div className="flex items-center gap-3">
          <div
            className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0"
            style={{ background: 'var(--ff-emerald)' }}
          >
            <span style={{ color: 'var(--ff-emerald-subtle)', fontWeight: 700, fontSize: 13 }}>FF</span>
          </div>
          <div>
            <p style={{ color: 'var(--ff-text-primary)', fontWeight: 600, fontSize: 14 }}>FinanceFlow</p>
            <p style={{ color: 'var(--ff-text-muted)', fontSize: 11 }}>Gestão Financeira</p>
          </div>
        </div>
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

        {/* Link Admin — visível apenas para Admins */}
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
              Painel Admin
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
          Configurações
        </NavLink>
      </div>
    </aside>
  )
}