import { useState } from 'react'
import { Trash2, Eye, EyeOff, Loader2 } from 'lucide-react'
import { useDeleteAccount } from '../api/useSettings'

interface DeleteAccountDialogProps {
  onClose: () => void
}

const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '10px',
  padding:      '9px 14px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

export default function DeleteAccountDialog({ onClose }: DeleteAccountDialogProps) {
  const [password,     setPassword]     = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const deleteAccount = useDeleteAccount()

  function handleConfirm() {
    if (!password) return
    deleteAccount.mutate({ currentPassword: password })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div
        className="w-full max-w-md rounded-2xl shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid rgba(244,63,94,0.3)' }}
      >
        {/* Header */}
        <div
          className="flex items-center gap-3 px-6 py-4"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0"
            style={{ background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.2)' }}
          >
            <Trash2 size={18} style={{ color: 'var(--ff-expense)' }} />
          </div>
          <div>
            <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
              Excluir conta
            </h2>
            <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
              Esta ação é permanente e não pode ser desfeita
            </p>
          </div>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4">
          <p className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
            Todos os seus dados serão removidos permanentemente, incluindo transações,
            categorias, orçamentos e relatórios.
          </p>

          <div className="space-y-1.5">
            <label
              htmlFor="delete-password"
              className="block text-sm"
              style={{ color: 'var(--ff-text-secondary)' }}
            >
              Confirme a sua senha para continuar
            </label>
            <div className="relative">
              <input
                id="delete-password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                value={password}
                onChange={e => setPassword(e.target.value)}
                style={{ ...inputStyle, paddingRight: '44px' }}
                onFocus={e => (e.target.style.borderColor = 'var(--ff-expense)')}
                onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
              />
              <button
                type="button"
                onClick={() => setShowPassword(p => !p)}
                className="absolute right-3 top-1/2 -translate-y-1/2 transition-colors"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
                onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
              >
                {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div
          className="flex gap-3 px-6 py-4"
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          <button
            onClick={onClose}
            disabled={deleteAccount.isPending}
            className="flex-1 h-10 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
            style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
            onMouseEnter={e => (e.currentTarget.style.background = '#222222')}
            onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
          >
            Cancelar
          </button>
          <button
            onClick={handleConfirm}
            disabled={deleteAccount.isPending || !password}
            className="flex-1 h-10 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
            style={{ background: 'var(--ff-expense)', color: '#fff' }}
            onMouseEnter={e => { if (!deleteAccount.isPending) e.currentTarget.style.background = '#e11d48' }}
            onMouseLeave={e => { if (!deleteAccount.isPending) e.currentTarget.style.background = 'var(--ff-expense)' }}
          >
            {deleteAccount.isPending
              ? <><Loader2 size={14} className="animate-spin" />Excluindo...</>
              : 'Excluir conta'
            }
          </button>
        </div>
      </div>
    </div>
  )
}