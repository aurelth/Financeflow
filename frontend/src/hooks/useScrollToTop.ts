import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

// Scroll to top automático ao navegar entre páginas
export function useScrollToTop() {
  const { pathname } = useLocation()

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }, [pathname])
}