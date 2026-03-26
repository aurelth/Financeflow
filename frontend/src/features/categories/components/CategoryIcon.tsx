import {
  Utensils, Car, HeartPulse, House, GraduationCap,
  Gamepad2, Shirt, Monitor, Ellipsis, Briefcase,
  Laptop, TrendingUp, LucideProps,
} from 'lucide-react'

// Mapa de nomes Lucide → componente
const LUCIDE_ICONS: Record<string, React.FC<LucideProps>> = {
  'utensils':       Utensils,
  'car':            Car,
  'heart-pulse':    HeartPulse,
  'house':          House,
  'graduation-cap': GraduationCap,
  'gamepad-2':      Gamepad2,
  'shirt':          Shirt,
  'monitor':        Monitor,
  'ellipsis':       Ellipsis,
  'briefcase':      Briefcase,
  'laptop':         Laptop,
  'trending-up':    TrendingUp,
}

interface CategoryIconProps {
  icon:  string
  color: string
  size?: number
}

export default function CategoryIcon({ icon, color, size = 20 }: CategoryIconProps) {
  // Se for um ícone Lucide conhecido, renderiza o componente
  const LucideIcon = LUCIDE_ICONS[icon]
  if (LucideIcon) {
    return <LucideIcon size={size} style={{ color }} />
  }

  // Caso contrário, assume que é um emoji e renderiza como texto
  return <span style={{ fontSize: size }}>{icon}</span>
}