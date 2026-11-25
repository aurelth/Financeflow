import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { SIDEBAR_SHORTCUT } from '@/lib/sidebarShortcut'
import { useSidebarStore } from '@/store/sidebarStore'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'

// Recolhe/expande o menu lateral do desktop. No celular quem abre o menu é o botão hambúrguer do cabeçalho.
export default function SidebarToggle({ className }: { className?: string }) {
  const { t } = useTranslation('common')
  const collapsed       = useSidebarStore(s => s.collapsed)
  const toggleCollapsed = useSidebarStore(s => s.toggleCollapsed)

  const label = collapsed ? t('nav.expandMenu') : t('nav.collapseMenu')
  const Icon  = collapsed ? PanelLeftOpen : PanelLeftClose

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            onClick={toggleCollapsed}
            aria-label={label}
            aria-expanded={!collapsed}
            className={cn(
              'w-9 h-9 flex items-center justify-center rounded-xl transition-colors',
              'hover:bg-[var(--ff-bg-elevated)] hover:text-[var(--ff-text-primary)]',
              'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ff-emerald)]',
              className
            )}
            style={{ color: 'var(--ff-text-muted)' }}
          >
            <Icon size={20} />
          </button>
        </TooltipTrigger>
        <TooltipContent side="bottom" align="start">
          {label}
          <kbd
            className="ml-2 rounded px-1 py-px text-[11px] font-normal"
            style={{ color: 'var(--ff-text-muted)', border: '1px solid var(--ff-border)' }}
          >
            {SIDEBAR_SHORTCUT}
          </kbd>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
