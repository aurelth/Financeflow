// Reduz a foto no navegador antes de enviar: fotos de celular passam de 5 MB (limite do servidor)
// e o servidor reduz de qualquer forma. Se o navegador não conseguir (HEIC, sem canvas, jsdom),
// devolve o arquivo original e deixa o servidor decidir.
const MAX_EDGE = 1568
const QUALITY  = 0.85

export async function downscaleImage(file: File): Promise<File> {
  if (typeof createImageBitmap !== 'function' || typeof document === 'undefined') return file

  try {
    const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
    const scale  = Math.min(1, MAX_EDGE / Math.max(bitmap.width, bitmap.height))

    // Já é pequena e leve: mandar o original evita recompressão sem ganho
    if (scale === 1 && file.size <= 1_000_000 && file.type === 'image/jpeg') {
      bitmap.close()
      return file
    }

    const canvas  = document.createElement('canvas')
    canvas.width  = Math.round(bitmap.width * scale)
    canvas.height = Math.round(bitmap.height * scale)
    const ctx = canvas.getContext('2d')
    if (!ctx) { bitmap.close(); return file }

    ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    bitmap.close()

    const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/jpeg', QUALITY))
    if (!blob || (blob.size >= file.size && scale === 1)) return file

    const name = file.name.replace(/\.[^.]+$/, '') || 'comprovante'
    return new File([blob], `${name}.jpg`, { type: 'image/jpeg' })
  } catch {
    return file
  }
}
