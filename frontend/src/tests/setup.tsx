import '@testing-library/jest-dom'
import React from 'react'
import { vi } from 'vitest'

// Mock global do react-i18next
const translations: Record<string, string> = {
  'actions.save':            'Guardar',
  'actions.cancel':          'Cancelar',
  'actions.delete':          'Eliminar',
  'actions.edit':            'Editar',
  'actions.create':          'Criar',
  'actions.confirm':         'Confirmar',
  'actions.loading':         'Carregando...',
  'actions.logout':          'Sair',
  'nav.dashboard':           'Dashboard',
  'nav.transactions':        'Transações',
  'nav.categories':          'Categorias',
  'nav.budgets':             'Orçamentos',
  'nav.reports':             'Relatórios',
  'nav.exports':             'Exportar',
  'nav.imports':             'Importar OFX',
  'nav.settings':            'Configurações',
  'nav.profile':             'Perfil',
  'nav.admin':               'Administração',
  'nav.comparison':          'Comparativo',
  'login.title':             'Bem-vindo de volta',
  'login.subtitle':          'Entre na sua conta para continuar',
  'login.email':             'Email',
  'login.password':          'Senha',
  'login.forgotPassword':    'Esqueceu a senha?',
  'login.submit':            'Entrar',
  'login.noAccount':         'Não tem uma conta?',
  'login.register':          'Criar conta',
  'register.title':          'Criar sua conta',
  'register.subtitle':       'Comece a controlar suas finanças hoje',
  'register.name':           'Nome completo',
  'register.email':          'Email',
  'register.password':       'Senha',
  'register.cpf':            'CPF',
  'register.gender':         'Gênero',
  'register.genderMale':     'Masculino',
  'register.genderFemale':   'Feminino',
  'register.submit':         'Criar conta',
  'register.hasAccount':     'Já tem uma conta?',
  'register.login':          'Entrar',
  'profile.title':           'Perfil',
  'profile.subtitle':        'Gerencie suas informações e preferências',
  'profile.identity':        'Dados pessoais',
  'profile.preferences':     'Preferências',
  'profile.security':        'Alterar senha',
  'profile.name':            'Nome completo',
  'profile.email':           'Email',
  'profile.cpf':             'CPF',
  'profile.gender':          'Gênero',
  'profile.currency':        'Moeda',
  'profile.timezone':        'Fuso horário',
  'profile.language':        'Idioma',
  'profile.currentPassword': 'Senha atual',
  'profile.newPassword':     'Nova senha',
  'profile.confirmPassword': 'Confirmar senha',
  'profile.savePreferences': 'Salvar preferências',
  'profile.changePassword':  'Alterar senha',
  'page.title':              'Configurações',
  'page.subtitle':           'Gerencie as preferências do sistema e da sua conta',
  'sections.language':       'Idioma',
  'sections.notifications':  'Notificações',
  'language.subtitle':       'Escolha o idioma para textos e formatação',
  'nav.assistant':           'Assistente IA',
}

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const withoutNs = key.includes(':') ? key.split(':')[1] : key
      return translations[withoutNs] ?? withoutNs
    },
    i18n: {
      changeLanguage: vi.fn(),
      language: 'pt-BR',
    },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}))

// Mock do framer-motion com JSX
vi.mock('framer-motion', () => ({
  motion: new Proxy({}, {
    get: (_target, tag: string) => {
      const Component = ({
        children,
        initial:    _i,
        animate:    _a,
        exit:       _e,
        transition: _t,
        variants:   _v,
        whileHover: _wh,
        whileTap:   _wt,
        ...props
      }: any) => React.createElement(tag, props, children)
      Component.displayName = `motion.${tag}`
      return Component
    },
  }),
  AnimatePresence: ({ children }: any) => <>{children}</>,
  useAnimation:    () => ({ start: vi.fn() }),
  useInView:       () => true,
}))

// Mock do driver.js
vi.mock('driver.js', () => ({
  driver: vi.fn(() => ({
    setSteps:     vi.fn(),
    setConfig:    vi.fn(),
    drive:        vi.fn(),
    destroy:      vi.fn(),
    moveNext:     vi.fn(),
    movePrevious: vi.fn(),
  })),
}))

// Mock do lib/driver
vi.mock('@/lib/driver', () => ({
  createDriver: vi.fn(() => ({
    setSteps:  vi.fn(),
    setConfig: vi.fn(),
    drive:     vi.fn(),
    destroy:   vi.fn(),
  })),
}))

// Mock do scrollIntoView
window.HTMLElement.prototype.scrollIntoView = vi.fn()