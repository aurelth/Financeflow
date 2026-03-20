import { Bell, ChevronDown, LogOut, User } from 'lucide-react'
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
import { Button } from '@/components/ui/button'
import { useNavigate } from 'react-router-dom'

export default function Header() {
  const { user }   = useAuthStore()
  const { mutate: logout } = useLogout()
  const navigate   = useNavigate()

  const initials = user?.name
    .split(' ')
    .map(n => n[0])
    .slice(0, 2)
    .join('')
    .toUpperCase() ?? 'U'

  return (
    <header className="h-16 bg-slate-900 border-b border-slate-800 px-6 flex items-center justify-between">

      {/* Título da página será injetado dinamicamente futuramente */}
      <div />

      {/* Ações */}
      <div className="flex items-center gap-3">

        {/* Notificações */}
        <Button
          variant="ghost"
          size="icon"
          className="relative text-slate-400 hover:text-slate-200 hover:bg-slate-800"
        >
          <Bell size={18} />
          {/* Badge — será ativado na Fase 8 */}
        </Button>

        {/* Avatar + Dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className="flex items-center gap-2 px-3 py-2 rounded-xl hover:bg-slate-800 transition-colors">
              <div className="w-8 h-8 bg-indigo-500 rounded-full flex items-center justify-center text-white text-xs font-semibold">
                {initials}
              </div>
              <div className="text-left hidden sm:block">
                <p className="text-slate-200 text-sm font-medium leading-none">{user?.name}</p>
                <p className="text-slate-400 text-xs mt-0.5">{user?.email}</p>
              </div>
              <ChevronDown size={14} className="text-slate-400" />
            </button>
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end" className="w-52 bg-slate-900 border-slate-800">
            <DropdownMenuLabel className="text-slate-400 text-xs">Minha Conta</DropdownMenuLabel>
            <DropdownMenuSeparator className="bg-slate-800" />
            <DropdownMenuItem
              onClick={() => navigate('/profile')}
              className="text-slate-300 hover:text-white hover:bg-slate-800 cursor-pointer"
            >
              <User size={14} className="mr-2" />
              Perfil
            </DropdownMenuItem>
            <DropdownMenuSeparator className="bg-slate-800" />
            <DropdownMenuItem
              onClick={() => logout()}
              className="text-red-400 hover:text-red-300 hover:bg-slate-800 cursor-pointer"
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