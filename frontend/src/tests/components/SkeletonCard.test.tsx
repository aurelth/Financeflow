import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import SkeletonCard from '@/components/ui/SkeletonCard'

describe('SkeletonCard', () => {
  it('deve renderizar sem erros', () => {
    const { container } = render(<SkeletonCard />)
    expect(container.firstChild).toBeInTheDocument()
  })

  it('deve aplicar altura customizada', () => {
    const { container } = render(<SkeletonCard height="h-48" />)
    expect(container.firstChild).toHaveClass('h-48')
  })

  it('deve renderizar o número correcto de linhas', () => {
    const { container } = render(<SkeletonCard lines={5} />)
    const lines = container.querySelectorAll('.animate-pulse > div')
    expect(lines.length).toBe(5)
  })
})