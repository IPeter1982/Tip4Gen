// Square-crops a user-picked image to the shorter side, scales to 128×128,
// re-encodes as JPEG q=0.85, returns a data URL ready to POST as JSON.
// Used by both the personal avatar upload (/me) and the admin AI-avatar page.
export async function resizeToDataUrl(file: File): Promise<string> {
  const original = await readAsDataUrl(file)
  const img = await loadImage(original)
  const side = Math.min(img.naturalWidth, img.naturalHeight)
  const sx = Math.floor((img.naturalWidth - side) / 2)
  const sy = Math.floor((img.naturalHeight - side) / 2)
  const canvas = document.createElement('canvas')
  canvas.width = 128
  canvas.height = 128
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('Canvas nem támogatott a böngészőben.')
  ctx.drawImage(img, sx, sy, side, side, 0, 0, 128, 128)
  return canvas.toDataURL('image/jpeg', 0.85)
}

function readAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = () => reject(new Error('Nem sikerült beolvasni a fájlt.'))
    reader.readAsDataURL(file)
  })
}

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.onload = () => resolve(img)
    img.onerror = () => reject(new Error('Érvénytelen képfájl.'))
    img.src = src
  })
}
