import { ChevronDown, LogOut, User } from 'lucide-react'
import { useAuthStore } from '@/store/authStore'
import { useLogout } from '@/features/auth/api/useAuth'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useNavigate } from 'react-router-dom'
import NotificationDropdown from '@/features/notifications/components/NotificationDropdown'

export default function Header() {
  const { user }           = useAuthStore()
  const { mutate: logout } = useLogout()
  const navigate           = useNavigate()

  const initials = user?.name
    .split(' ')
    .map(n => n[0])
    .slice(0, 2)
    .join('')
    .toUpperCase() ?? 'U'

  return (
    <header
      className="h-16 px-6 flex items-center justify-between"
      style={{
        background:   'var(--ff-bg-card)',
        borderBottom: '1px solid var(--ff-border)',
      }}
    >
      <div />

      <div className="flex items-center gap-3">
        <NotificationDropdown />

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              className="flex items-center gap-2 px-3 py-2 rounded-xl transition-colors"
              style={{ color: 'var(--ff-text-secondary)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
            >
              <div
                className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-semibold flex-shrink-0"
                style={{ // Avatar verde esmeralda
                  background: 'var(--ff-emerald-subtle)',
                  color:      'var(--ff-emerald)',
                }}
              >
                {initials}
              </div>
              <div className="text-left hidden sm:block">
                <p className="text-sm font-medium leading-none" style={{ color: 'var(--ff-text-primary)' }}>
                  {user?.name}
                </p>
                <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                  {user?.email}
                </p>
              </div>
              <ChevronDown size={14} style={{ color: 'var(--ff-text-muted)' }} />
            </button>
          </DropdownMenuTrigger>

          <DropdownMenuContent
            align="end"
            className="w-52"
            style={{
              background:   'var(--ff-bg-card)',
              border:       '1px solid var(--ff-border)',
            }}
          >
            <DropdownMenuLabel style={{ color: 'var(--ff-text-muted)', fontSize: 11 }}>
              Minha Conta
            </DropdownMenuLabel>
            <DropdownMenuSeparator style={{ background: 'var(--ff-border)' }} />
            <DropdownMenuItem
              onClick={() => navigate('/profile')}
              className="cursor-pointer"
              style={{ color: 'var(--ff-text-secondary)' }}
            >
              <User size={14} className="mr-2" />
              Perfil
            </DropdownMenuItem>
            <DropdownMenuSeparator style={{ background: 'var(--ff-border)' }} />
            <DropdownMenuItem
              onClick={() => logout()}
              className="cursor-pointer"
              style={{ color: 'var(--ff-expense)' }}
            >
              <LogOut size={14} className="mr-2" />
              Sair
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}