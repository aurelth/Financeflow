import { describe, it, expect, vi, afterEach } from 'vitest'
import { downscaleImage } from '@/features/quick-entry/lib/downscaleImage'

const file = (size: number, type = 'image/png', name = 'recibo.png') =>
  new File([new Uint8Array(size)], name, { type })

function stubBitmap(width: number, height: number) {
  const close = vi.fn()
  vi.stubGlobal('createImageBitmap', vi.fn().mockResolvedValue({ width, height, close }))
  return close
}

function stubCanvas(blobSize: number | null) {
  const draw = vi.fn()
  const canvas = {
    width: 0, height: 0,
    getContext: () => ({ drawImage: draw }),
    toBlob: (cb: (b: Blob | null) => void) =>
      cb(blobSize === null ? null : new Blob([new Uint8Array(blobSize)], { type: 'image/jpeg' })),
  }
  const real = document.createElement.bind(document)
  vi.spyOn(document, 'createElement').mockImplementation(((tag: string) =>
    tag === 'canvas' ? canvas : real(tag)) as typeof document.createElement)
  return canvas
}

describe('downscaleImage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('devolve o original quando o navegador não sabe decodificar (sem createImageBitmap)', async () => {
    vi.stubGlobal('createImageBitmap', undefined)
    const original = file(100)
    expect(await downscaleImage(original)).toBe(original)
  })

  it('devolve o original quando a decodificação falha (ex.: HEIC)', async () => {
    vi.stubGlobal('createImageBitmap', vi.fn().mockRejectedValue(new Error('unsupported')))
    const original = file(100, 'image/heic', 'foto.heic')
    expect(await downscaleImage(original)).toBe(original)
  })

  it('não recomprime um JPEG pequeno que já cabe no limite', async () => {
    const close = stubBitmap(800, 600)
    const original = file(200_000, 'image/jpeg', 'foto.jpg')

    expect(await downscaleImage(original)).toBe(original)
    expect(close).toHaveBeenCalled()
  })

  it('reduz a maior aresta para 1568 px mantendo a proporção e entrega JPEG', async () => {
    stubBitmap(4000, 3000)
    const canvas = stubCanvas(300_000)

    const result = await downscaleImage(file(4_000_000, 'image/png', 'cupom.png'))

    expect([canvas.width, canvas.height]).toEqual([1568, 1176])
    expect(result.type).toBe('image/jpeg')
    expect(result.name).toBe('cupom.jpg')
    expect(result.size).toBe(300_000)
  })

  it('devolve o original quando o canvas não gera imagem', async () => {
    stubBitmap(4000, 3000)
    stubCanvas(null)
    const original = file(4_000_000)

    expect(await downscaleImage(original)).toBe(original)
  })
})
