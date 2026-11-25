import { useEffect } from 'react'
import { useSidebarStore } from '@/store/sidebarStore'

// Ctrl+B (⌘B no Mac) abre/recolhe o menu, como nos editores
const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.platform)
export const SIDEBAR_SHORTCUT = isMac ? '⌘B' : 'Ctrl+B'

function isEditable(target: EventTarget | null) {
  const el = target as HTMLElement | null
  if (!el) return false
  return el.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(el.tagName)
}

// Monte uma única vez (no layout): escuta o atalho enquanto o app está aberto,
// sem atrapalhar quem está digitando
export function useSidebarShortcut() {
  const toggleCollapsed = useSidebarStore(s => s.toggleCollapsed)

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (!(e.ctrlKey || e.metaKey) || e.shiftKey || e.altKey) return
      if (e.key.toLowerCase() !== 'b' || isEditable(e.target)) return
      e.preventDefault()
      toggleCollapsed()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [toggleCollapsed])
}
