/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './index.html',
    './src/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        background:  'hsl(var(--background))',
        foreground:  'hsl(var(--foreground))',
        primary: {
          DEFAULT:    'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT:    'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        muted: {
          DEFAULT:    'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT:    'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        destructive: {
          DEFAULT:    'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        card: {
          DEFAULT:    'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
        popover: {
          DEFAULT:    'hsl(var(--popover))',
          foreground: 'hsl(var(--popover-foreground))',
        },
        border:  'hsl(var(--border))',
        input:   'hsl(var(--input))',
        ring:    'hsl(var(--ring))',

        // Adicionado: paleta FinanceFlow
        ff: {
          base:           'var(--ff-bg-base)',
          card:           'var(--ff-bg-card)',
          elevated:       'var(--ff-bg-elevated)',
          border:         'var(--ff-border)',
          'border-subtle': 'var(--ff-border-subtle)',
          emerald:        'var(--ff-emerald)',
          'emerald-hover': 'var(--ff-emerald-hover)',
          'emerald-subtle': 'var(--ff-emerald-subtle)',
          'emerald-text': 'var(--ff-emerald-text)',
          'text-primary':   'var(--ff-text-primary)',
          'text-secondary': 'var(--ff-text-secondary)',
          'text-muted':     'var(--ff-text-muted)',
          income:    'var(--ff-income)',
          expense:   'var(--ff-expense)',
          pending:   'var(--ff-pending)',
          scheduled: 'var(--ff-scheduled)',
        },
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
    },
  },
  plugins: [],
}