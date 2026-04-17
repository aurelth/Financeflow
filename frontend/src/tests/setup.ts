import '@testing-library/jest-dom'
import { vi } from 'vitest'

// Dicionário de traduções para testes (pt-BR)
const translations: Record<string, string> = {
  // common
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
  // auth
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
  // settings
  'page.title':              'Configurações',
  'page.subtitle':           'Gerencie as preferências do sistema e da sua conta',
  'sections.language':       'Idioma',
  'sections.notifications':  'Notificações',
  'language.subtitle':       'Escolha o idioma para textos e formatação',
}

// Mock global do react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      // Remove namespace (ex: 'common:actions.save' → 'actions.save')
      const withoutNs = key.includes(':') ? key.split(':')[1] : key
      return translations[withoutNs] ?? withoutNs
    },
    i18n: {
      changeLanguage: vi.fn(),
      language: 'pt-BR',
    },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}))

// Mock do driver.js
vi.mock('driver.js', () => ({
  driver: vi.fn(() => ({
    setSteps:    vi.fn(),
    setConfig:   vi.fn(),
    drive:       vi.fn(),
    destroy:     vi.fn(),
    moveNext:    vi.fn(),
    movePrevious: vi.fn(),
  })),
}))

// Mock do lib/driver
vi.mock('@/lib/driver', () => ({
  createDriver: vi.fn(() => ({
    setSteps:    vi.fn(),
    setConfig:   vi.fn(),
    drive:       vi.fn(),
    destroy:     vi.fn(),
  })),
}))

// Adicionado: mock do framer-motion
vi.mock('framer-motion', () => {
  const createComponent = (tag: string) =>
    ({ children, ...props }: any) => {      
      const { initial, animate, exit, transition, variants, whileHover, whileTap, ...rest } = props
      return Object.assign(document.createElement(tag), rest, { children })
    }

  return {
    motion: {
      div:     createComponent('div'),
      section: createComponent('section'),
      ul:      createComponent('ul'),
      li:      createComponent('li'),
      span:    createComponent('span'),
      p:       createComponent('p'),
    },
    AnimatePresence: ({ children }: any) => children,
    useAnimation:    () => ({ start: vi.fn() }),
    useInView:       () => true,
  }
})