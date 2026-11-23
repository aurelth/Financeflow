import { Landmark, PiggyBank, CreditCard, Wallet, TrendingUp } from 'lucide-react'
import type { AccountType } from '../types/account.types'

const ICONS: Record<AccountType, React.ElementType> = {
  Checking:   Landmark,
  Savings:    PiggyBank,
  CreditCard: CreditCard,
  Cash:       Wallet,
  Investment: TrendingUp,
}

interface AccountIconProps {
  type:  AccountType
  color: string
  size?: number
}

export default function AccountIcon({ type, color, size = 20 }: AccountIconProps) {
  const Icon = ICONS[type] ?? Landmark
  return <Icon size={size} style={{ color }} />
}
