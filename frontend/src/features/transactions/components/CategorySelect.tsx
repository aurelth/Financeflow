import { useState } from 'react'
import { Check, ChevronsUpDown, Search } from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import CategoryIcon from '../../categories/components/CategoryIcon'
import type { Category } from '../../categories/types/category.types'

interface CategorySelectProps {
  categories:   Category[]
  value:        string
  onChange:     (value: string) => void
  placeholder?: string
  nullable?:    boolean
  nullLabel?:   string
}

export default function CategorySelect({
  categories,
  value,
  onChange,
  placeholder = 'Selecionar categoria',
  nullable    = false,
  nullLabel   = 'Todas as categorias',
}: CategorySelectProps) {
  const [open, setOpen]       = useState(false)
  const [search, setSearch]   = useState('')

  const selected = categories.find(c => c.id === value)

  const filtered = categories.filter(c =>
    c.name.toLowerCase().includes(search.toLowerCase())
  )

  function handleSelect(id: string) {
    onChange(id)
    setOpen(false)
    setSearch('')
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          className={cn(
            'w-full flex items-center justify-between gap-2',
            'bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5',
            'text-sm transition-colors focus:outline-none',
            open ? 'border-indigo-500' : 'hover:border-slate-600',
            selected ? 'text-slate-200' : 'text-slate-500'
          )}
        >
          <span className="flex items-center gap-2 min-w-0">
            {selected ? (
              <>
                <CategoryIcon icon={selected.icon} color={selected.color} size={14} />
                <span className="truncate">{selected.name}</span>
              </>
            ) : (
              <span>{placeholder}</span>
            )}
          </span>
          <ChevronsUpDown size={14} className="text-slate-500 flex-shrink-0" />
        </button>
      </PopoverTrigger>

      <PopoverContent
        className="p-0 bg-slate-900 border border-slate-700 rounded-xl shadow-xl w-[var(--radix-popover-trigger-width)]"
        align="start"
        sideOffset={4}
      >
        {/* Campo de pesquisa */}
        <div className="flex items-center gap-2 px-3 py-2.5 border-b border-slate-700">
          <Search size={13} className="text-slate-500 flex-shrink-0" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Pesquisar..."
            autoFocus
            className="flex-1 bg-transparent text-sm text-slate-200 placeholder-slate-500 focus:outline-none"
          />
        </div>

        {/* Lista */}
        <div className="max-h-56 overflow-y-auto py-1">
          {nullable && (
            <button
              onClick={() => handleSelect('')}
              className={cn(
                'w-full flex items-center gap-2 px-3 py-2 text-sm transition-colors',
                !value
                  ? 'text-indigo-400 bg-indigo-500/10'
                  : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'
              )}
            >
              <Check size={13} className={cn('flex-shrink-0', !value ? 'opacity-100' : 'opacity-0')} />
              {nullLabel}
            </button>
          )}

          {filtered.length === 0 ? (
            <p className="text-slate-500 text-sm text-center py-4">
              Nenhuma categoria encontrada.
            </p>
          ) : (
            filtered.map(c => (
              <button
                key={c.id}
                onClick={() => handleSelect(c.id)}
                className={cn(
                  'w-full flex items-center gap-2 px-3 py-2 text-sm transition-colors',
                  value === c.id
                    ? 'text-indigo-400 bg-indigo-500/10'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-slate-200'
                )}
              >
                <Check size={13} className={cn('flex-shrink-0', value === c.id ? 'opacity-100' : 'opacity-0')} />
                <CategoryIcon icon={c.icon} color={c.color} size={14} />
                <span>{c.name}</span>
              </button>
            ))
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}