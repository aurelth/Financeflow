import { useState } from 'react'
import { Search, ShieldCheck, ShieldOff, UserCheck, UserX, Loader2, ChevronLeft, ChevronRight } from 'lucide-react'
import {
  useAdminUsers,
  useDeactivateUser,
  useReactivateUser,
  usePromoteUser,
  useDemoteUser,
} from '../api/useAdmin'
import type { AdminUser } from '../types/admin.types'

const badgeStyle = (isActive: boolean, role: string) => {
  if (role === 'Admin') return { background: 'rgba(16,185,129,0.12)', color: 'var(--ff-emerald)', border: '1px solid rgba(16,185,129,0.3)' }
  if (!isActive)        return { background: 'rgba(244,63,94,0.1)',   color: 'var(--ff-expense)',  border: '1px solid rgba(244,63,94,0.2)'  }
  return                       { background: 'rgba(99,102,241,0.1)',  color: '#818cf8',             border: '1px solid rgba(99,102,241,0.2)' }
}

const genderLabel = (g: string) => g === 'Male' ? 'Masculino' : g === 'Female' ? 'Feminino' : '—'

interface ConfirmAction {
  type:   'deactivate' | 'reactivate' | 'promote' | 'demote'
  user:   AdminUser
}

export default function AdminUsersPage() {
  const [page,     setPage]     = useState(1)
  const [search,   setSearch]   = useState('')
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined)
  const [confirm,  setConfirm]  = useState<ConfirmAction | null>(null)

  const { data, isLoading } = useAdminUsers(page, 20, search || undefined, isActive)

  const deactivate = useDeactivateUser()
  const reactivate = useReactivateUser()
  const promote    = usePromoteUser()
  const demote     = useDemoteUser()

  const isPending =
    deactivate.isPending || reactivate.isPending ||
    promote.isPending    || demote.isPending

  function handleConfirm() {
    if (!confirm) return
    const { type, user } = confirm

    const onSuccess = () => setConfirm(null)

    if (type === 'deactivate') deactivate.mutate(user.id, { onSuccess })
    if (type === 'reactivate') reactivate.mutate(user.id, { onSuccess })
    if (type === 'promote')    promote.mutate(user.id,    { onSuccess })
    if (type === 'demote')     demote.mutate(user.id,     { onSuccess })
  }

  const confirmLabels: Record<ConfirmAction['type'], string> = {
    deactivate: 'Desativar usuário',
    reactivate: 'Reativar usuário',
    promote:    'Promover a Admin',
    demote:     'Rebaixar a usuário comum',
  }

  const confirmDescriptions: Record<ConfirmAction['type'], (name: string) => string> = {
    deactivate: name => `Deseja desativar a conta de ${name}? O usuário não conseguirá mais fazer login.`,
    reactivate: name => `Deseja reativar a conta de ${name}? O usuário poderá fazer login novamente.`,
    promote:    name => `Deseja promover ${name} a Admin? Ele terá acesso ao painel administrativo.`,
    demote:     name => `Deseja rebaixar ${name} a usuário comum? Ele perderá acesso ao painel administrativo.`,
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          Gestão de Usuários
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          Gerencie os usuários da plataforma
        </p>
      </div>

      {/* Filtros */}
      <div
        className="rounded-2xl p-4 flex flex-wrap gap-3 items-center"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="relative flex-1 min-w-48">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: 'var(--ff-text-muted)' }} />
          <input
            placeholder="Buscar por nome ou email..."
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1) }}
            className="w-full pl-9 pr-4 py-2 rounded-xl text-sm outline-none"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-primary)' }}
          />
        </div>

        <div className="flex gap-2">
          {([undefined, true, false] as const).map((val, i) => {
            const labels = ['Todos', 'Ativos', 'Inativos']
            const active = isActive === val
            return (
              <button
                key={i}
                onClick={() => { setIsActive(val); setPage(1) }}
                className="px-3 py-2 rounded-xl text-xs font-medium transition-colors"
                style={{
                  background: active ? 'var(--ff-emerald-subtle)' : 'var(--ff-bg-elevated)',
                  color:      active ? 'var(--ff-emerald)'         : 'var(--ff-text-muted)',
                  border:     active ? '1px solid rgba(16,185,129,0.3)' : '1px solid var(--ff-border)',
                }}
              >
                {labels[i]}
              </button>
            )
          })}
        </div>
      </div>

      {/* Tabela */}
      <div
        className="rounded-2xl overflow-hidden"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--ff-border)' }}>
                    {['Nome', 'Email', 'Gênero', 'Status', 'Criado em', 'Ações'].map(h => (
                      <th
                        key={h}
                        className="text-left px-4 py-3 font-medium text-xs uppercase tracking-wide"
                        style={{ color: 'var(--ff-text-muted)' }}
                      >
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {data?.users.map(user => (
                    <tr
                      key={user.id}
                      style={{ borderBottom: '1px solid var(--ff-border-subtle)' }}
                    >
                      <td className="px-4 py-3" style={{ color: 'var(--ff-text-primary)' }}>
                        {user.name}
                      </td>
                      <td className="px-4 py-3" style={{ color: 'var(--ff-text-secondary)' }}>
                        {user.email}
                      </td>
                      <td className="px-4 py-3" style={{ color: 'var(--ff-text-secondary)' }}>
                        {genderLabel(user.gender)}
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className="px-2 py-0.5 rounded-full text-xs font-medium"
                          style={badgeStyle(user.isActive, user.role)}
                        >
                          {user.role === 'Admin' ? 'Admin' : user.isActive ? 'Ativo' : 'Inativo'}
                        </span>
                      </td>
                      <td className="px-4 py-3" style={{ color: 'var(--ff-text-muted)' }}>
                        {new Date(user.createdAt).toLocaleDateString('pt-BR')}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1">
                          {user.isActive ? (
                            <button
                              onClick={() => setConfirm({ type: 'deactivate', user })}
                              className="p-1.5 rounded-lg transition-colors"
                              title="Desativar"
                              style={{ color: 'var(--ff-text-muted)' }}
                              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-expense)')}
                              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                            >
                              <UserX size={15} />
                            </button>
                          ) : (
                            <button
                              onClick={() => setConfirm({ type: 'reactivate', user })}
                              className="p-1.5 rounded-lg transition-colors"
                              title="Reativar"
                              style={{ color: 'var(--ff-text-muted)' }}
                              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
                              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                            >
                              <UserCheck size={15} />
                            </button>
                          )}

                          {user.isActive && user.role === 'User' && (
                            <button
                              onClick={() => setConfirm({ type: 'promote', user })}
                              className="p-1.5 rounded-lg transition-colors"
                              title="Promover a Admin"
                              style={{ color: 'var(--ff-text-muted)' }}
                              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
                              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                            >
                              <ShieldCheck size={15} />
                            </button>
                          )}

                          {user.isActive && user.role === 'Admin' && (
                            <button
                              onClick={() => setConfirm({ type: 'demote', user })}
                              className="p-1.5 rounded-lg transition-colors"
                              title="Rebaixar a usuário comum"
                              style={{ color: 'var(--ff-text-muted)' }}
                              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-expense)')}
                              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                            >
                              <ShieldOff size={15} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Paginação */}
            {data && data.totalPages > 1 && (
              <div
                className="flex items-center justify-between px-4 py-3"
                style={{ borderTop: '1px solid var(--ff-border)' }}
              >
                <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                  {data.total} usuário(s) encontrado(s)
                </p>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setPage(p => p - 1)}
                    disabled={page === 1}
                    className="p-1.5 rounded-lg transition-colors disabled:opacity-40"
                    style={{ color: 'var(--ff-text-muted)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                  >
                    <ChevronLeft size={16} />
                  </button>
                  <span className="text-xs" style={{ color: 'var(--ff-text-secondary)' }}>
                    {page} / {data.totalPages}
                  </span>
                  <button
                    onClick={() => setPage(p => p + 1)}
                    disabled={page === data.totalPages}
                    className="p-1.5 rounded-lg transition-colors disabled:opacity-40"
                    style={{ color: 'var(--ff-text-muted)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                  >
                    <ChevronRight size={16} />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Modal de confirmação */}
      {confirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div
            className="w-full max-w-md rounded-2xl shadow-2xl"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div className="px-6 py-4" style={{ borderBottom: '1px solid var(--ff-border)' }}>
              <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
                {confirmLabels[confirm.type]}
              </h2>
            </div>
            <div className="px-6 py-5">
              <p className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
                {confirmDescriptions[confirm.type](confirm.user.name)}
              </p>
            </div>
            <div className="flex gap-3 px-6 py-4" style={{ borderTop: '1px solid var(--ff-border)' }}>
              <button
                onClick={() => setConfirm(null)}
                disabled={isPending}
                className="flex-1 h-10 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
                style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirm}
                disabled={isPending}
                className="flex-1 h-10 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
                style={{
                  background: confirm.type === 'deactivate' || confirm.type === 'demote'
                    ? 'var(--ff-expense)' : 'var(--ff-emerald)',
                  color: confirm.type === 'deactivate' || confirm.type === 'demote'
                    ? '#fff' : 'var(--ff-emerald-subtle)',
                }}
              >
                {isPending
                  ? <><Loader2 size={14} className="animate-spin" />Processando...</>
                  : confirmLabels[confirm.type]
                }
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}