import { driver, type Config } from 'driver.js'
import 'driver.js/dist/driver.css'

// Cria instância do driver com tema alinhado ao FinanceFlow
export function createDriver(extra?: Partial<Config>) {
  return driver({
    animate:        true,
    smoothScroll:   true,
    allowClose:     true,
    overlayOpacity: 0.6,
    stagePadding:   8,
    stageRadius:    12,
    popoverClass:   'ff-driver-popover',
    nextBtnText:    '→',
    prevBtnText:    '←',
    doneBtnText:    '✓',
    showProgress:   true,
    progressText:   '{{current}} / {{total}}',
    ...extra,
  })
}