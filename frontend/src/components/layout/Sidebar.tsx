import { NavLink, useLocation, useMatch } from 'react-router-dom'
import { useEffect } from 'react'
import {
  LayoutDashboard, ArrowLeftRight, Tag, PiggyBank,
  BarChart3, GitCompare, FileText, Settings, ShieldCheck, Upload, X, TrendingUp, Sparkles, HeartPulse, Target, Landmark, CalendarDays,
  type LucideIcon,
} from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { cn } from '@/lib/utils'
import { useAuthStore }    from '@/store/authStore'
import { useTranslation }  from 'react-i18next'
import { useSidebarStore } from '@/store/sidebarStore'
import { useSidebarShortcut } from '@/lib/sidebarShortcut'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'

const navLinkStyle = ({ isActive }: { isActive: boolean }) =>
  isActive
    ? { background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald)' }
    : { color: 'var(--ff-text-muted)' }

const navLinkClass = (compact: boolean) => ({ isActive }: { isActive: boolean }) =>
  cn(
    'flex items-center h-11 rounded-xl text-sm font-medium transition-all duration-200',
    compact ? 'justify-center gap-0' : 'gap-3 px-3',
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

// No recolhido o nome some da tela: o balão ao lado do ícone o substitui (também no foco do teclado)
function WithTooltip({
  label, enabled, children,
}: { label: string; enabled: boolean; children: React.ReactElement }) {
  if (!enabled) return children

  return (
    <Tooltip>
      <TooltipTrigger asChild>{children}</TooltipTrigger>
      <TooltipContent side="right">
        {label}
      </TooltipContent>
    </Tooltip>
  )
}

function ItemLabel({ compact, children }: { compact: boolean; children: React.ReactNode }) {
  return (
    <span
      className={cn(
        'truncate transition-[max-width,opacity] duration-200 motion-reduce:transition-none',
        compact ? 'max-w-0 opacity-0 overflow-hidden' : 'max-w-[11rem] opacity-100'
      )}
    >
      {children}
    </span>
  )
}

interface SidebarLinkProps {
  to: string
  label: string
  compact: boolean
  // Ícone do item; use `leading` quando o item tem outro elemento à esquerda (ex.: avatar do perfil)
  icon?: LucideIcon
  leading?: React.ReactNode
}

function SidebarLink({ to, label, compact, icon: Icon, leading }: SidebarLinkProps) {
  // O Radix (asChild) junta className/style como texto, então o estado ativo é calculado aqui
  // e o NavLink recebe valores prontos, nunca funções
  const isActive = !!useMatch({ path: to, end: false })

  return (
    <WithTooltip label={label} enabled={compact}>
      <NavLink
        to={to}
        aria-label={compact ? label : undefined}
        className={navLinkClass(compact)({ isActive })}
        style={navLinkStyle({ isActive })}
        onMouseEnter={onNavEnter}
        onMouseLeave={onNavLeave}
      >
        {leading ?? (Icon && <Icon size={18} className="flex-shrink-0" />)}
        <ItemLabel compact={compact}>{label}</ItemLabel>
      </NavLink>
    </WithTooltip>
  )
}

function Divider() {
  return (
    <div
      className="my-2"
      style={{ borderTop: '1px solid var(--ff-border)', marginLeft: '-4px', marginRight: '-4px' }}
    />
  )
}

export default function Sidebar() {
  const { t }      = useTranslation('common')
  const user       = useAuthStore(s => s.user)
  const isAdmin    = user?.role === 'Admin'
  const location   = useLocation()
  const { isOpen, close, collapsed } = useSidebarStore()
  useSidebarShortcut()

  useEffect(() => {
    close()
  }, [location.pathname, close])

  const navItems = [
    { to: '/dashboard',    icon: LayoutDashboard, label: t('nav.dashboard')    },
    { to: '/transactions', icon: ArrowLeftRight,  label: t('nav.transactions') },
    { to: '/calendar',     icon: CalendarDays,    label: t('nav.calendar')     },
    { to: '/accounts',     icon: Landmark,        label: t('nav.accounts')     },
    { to: '/categories',   icon: Tag,             label: t('nav.categories')   },
    { to: '/budgets',      icon: PiggyBank,       label: t('nav.budgets')      },
    { to: '/goals',        icon: Target,          label: t('nav.goals')        },
    { to: '/comparison',   icon: GitCompare,      label: t('nav.comparison')   },
    { to: '/reports',      icon: BarChart3,       label: t('nav.reports')      },
    { to: '/exports',      icon: FileText,        label: t('nav.exports')      },
    { to: '/imports',      icon: Upload,          label: t('nav.imports')      },
    { to: '/health-score', icon: HeartPulse,      label: t('nav.healthScore')  },
  ]

  const apiUrl = import.meta.env.VITE_API_URL ?? ''

  // compact = só ícones. Só o desktop recolhe; a gaveta do celular sempre mostra o menu completo
  const renderContent = (compact: boolean) => {
    return (
      <aside
        className={cn(
          'flex flex-col h-full overflow-hidden transition-[width] duration-200 ease-out motion-reduce:transition-none',
          compact ? 'w-[72px]' : 'w-64'
        )}
        style={{ background: 'var(--ff-bg-card)', borderRight: '1px solid var(--ff-border)' }}
      >
        {/* Logo — mesma altura do cabeçalho, para a linha divisória ficar alinhada */}
        <div
          className={cn(
            'flex h-16 flex-shrink-0 items-center transition-[padding] duration-200 motion-reduce:transition-none',
            compact ? 'justify-center px-3' : 'justify-between px-6'
          )}
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <div className={cn('flex items-center min-w-0', compact ? 'gap-0' : 'gap-3')}>
            <div
              className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0"
              style={{ background: 'var(--ff-emerald)' }}
            >
              <TrendingUp size={18} style={{ color: 'var(--ff-emerald-subtle)' }} />
            </div>
            <div
              className={cn(
                'overflow-hidden whitespace-nowrap transition-[max-width,opacity] duration-200 motion-reduce:transition-none',
                compact ? 'max-w-0 opacity-0' : 'max-w-[10rem] opacity-100'
              )}
            >
              <p style={{ color: 'var(--ff-text-primary)', fontWeight: 600, fontSize: 14 }}>FinanceFlow</p>
              <p style={{ color: 'var(--ff-text-muted)', fontSize: 11 }}>Gestão Financeira</p>
            </div>
          </div>

          {/* Botão fechar em mobile */}
          <button
            onClick={close}
            aria-label={t('nav.closeMenu')}
            className="lg:hidden p-1.5 rounded-lg transition-colors"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
          >
            <X size={18} />
          </button>
        </div>

        {/* Navegação principal */}
        <nav
          className={cn(
            'flex-1 space-y-0.5 overflow-y-auto overflow-x-hidden transition-[padding] duration-200 motion-reduce:transition-none',
            compact ? 'p-3' : 'p-4'
          )}
        >
          {navItems.map(({ to, icon, label }) => (
            <SidebarLink key={to} to={to} icon={icon} label={label} compact={compact} />
          ))}

          {/* Divisor e link do Assistente IA */}
          <Divider />
          <SidebarLink to="/assistant" icon={Sparkles} label={t('nav.assistant')} compact={compact} />

          {isAdmin && (
            <>
              <Divider />
              <SidebarLink to="/admin" icon={ShieldCheck} label={t('nav.admin')} compact={compact} />
            </>
          )}
        </nav>

        {/* Perfil e Configurações */}
        <div
          className={cn('space-y-1 transition-[padding] duration-200 motion-reduce:transition-none', compact ? 'p-3' : 'p-4')}
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          {/* Perfil com avatar */}
          <SidebarLink
            to="/profile"
            label={user?.name ?? t('nav.profile')}
            compact={compact}
            leading={
              <div
                className="w-7 h-7 rounded-full overflow-hidden flex items-center justify-center text-xs font-bold flex-shrink-0"
                style={{
                  background: user?.avatarUrl ? 'transparent' : 'var(--ff-emerald)',
                  color:      'var(--ff-emerald-subtle)',
                  border:     '1px solid var(--ff-border)',
                }}
              >
                {user?.avatarUrl
                  ? <img
                      src={`${apiUrl}/uploads/avatars/${user.avatarUrl}`}
                      alt={user.name}
                      className="w-full h-full object-cover"
                    />
                  : user?.name?.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase()
                }
              </div>
            }
          />

          {/* Configurações */}
          <SidebarLink to="/settings" icon={Settings} label={t('nav.settings')} compact={compact} />
        </div>
      </aside>
    )
  }

  return (
    <TooltipProvider>
      {/* Desktop — sidebar sempre visível (aberta ou recolhida) */}
      <div className="hidden lg:flex">
        {renderContent(collapsed)}
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
              {renderContent(false)}
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </TooltipProvider>
  )
}
