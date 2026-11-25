import { create } from 'zustand'

const COLLAPSED_KEY = 'ff:sidebar-collapsed'

// Preferência do desktop: o navegador pode bloquear o armazenamento (aba anônima), então nunca quebra a tela
function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === '1'
  } catch {
    return false
  }
}

function writeCollapsed(value: boolean) {
  try {
    localStorage.setItem(COLLAPSED_KEY, value ? '1' : '0')
  } catch {
    // sem armazenamento: a escolha vale só nesta sessão
  }
}

interface SidebarState {
  // Gaveta do celular/tablet
  isOpen: boolean
  open:   () => void
  close:  () => void
  toggle: () => void
  // Menu do desktop recolhido (só ícones). Lembrado entre acessos
  collapsed:       boolean
  toggleCollapsed: () => void
}

export const useSidebarStore = create<SidebarState>(set => ({
  isOpen: false,
  open:   () => set({ isOpen: true }),
  close:  () => set({ isOpen: false }),
  toggle: () => set(s => ({ isOpen: !s.isOpen })),

  collapsed: readCollapsed(),
  toggleCollapsed: () =>
    set(s => {
      const next = !s.collapsed
      writeCollapsed(next)
      return { collapsed: next }
    }),
}))
