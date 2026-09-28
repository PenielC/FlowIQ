// Render at a fixed desktop layout so a phone gets the same document as a laptop.
const RENDER_WINDOW_WIDTH = 1024
const RENDER_SCALE = 2
const PAGE_MARGIN_PT = 28
const MIN_PAGE_PX = 80

export async function renderInvoicePdf(element: HTMLElement, filename: string): Promise<File> {
  const [{ default: html2canvas }, { jsPDF }] = await Promise.all([import('html2canvas-pro'), import('jspdf')])
  // Safe places to start a new page (bottom edges of table rows and top-level blocks), measured
  // in the same cloned layout that gets rendered so a page never splits a line item in half.
  const breakpoints: number[] = []
  const canvas = await html2canvas(element, {
    scale: RENDER_SCALE,
    backgroundColor: '#ffffff',
    windowWidth: RENDER_WINDOW_WIDTH,
    onclone: (_doc, cloned) => {
      const top = cloned.getBoundingClientRect().top
      cloned.querySelectorAll(':scope > *, tr').forEach((el) => {
        breakpoints.push(Math.round((el.getBoundingClientRect().bottom - top) * RENDER_SCALE))
      })
      breakpoints.sort((a, b) => a - b)
    },
  })

  const pdf = new jsPDF({ unit: 'pt', format: 'a4' })
  const contentWidth = pdf.internal.pageSize.getWidth() - PAGE_MARGIN_PT * 2
  const contentHeight = pdf.internal.pageSize.getHeight() - PAGE_MARGIN_PT * 2
  const pxPerPt = canvas.width / contentWidth
  const sliceHeightPx = Math.floor(contentHeight * pxPerPt)

  let y = 0
  for (let page = 0; y < canvas.height; page++) {
    const limit = y + sliceHeightPx
    const end = limit >= canvas.height ? canvas.height : (breakpoints.filter((b) => b > y + MIN_PAGE_PX && b <= limit).pop() ?? limit)
    const heightPx = end - y
    const slice = document.createElement('canvas')
    slice.width = canvas.width
    slice.height = heightPx
    const ctx = slice.getContext('2d')
    if (!ctx) throw new Error('Canvas is not available')
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(0, 0, slice.width, slice.height)
    ctx.drawImage(canvas, 0, y, canvas.width, heightPx, 0, 0, canvas.width, heightPx)

    if (page > 0) pdf.addPage()
    pdf.addImage(slice.toDataURL('image/jpeg', 0.95), 'JPEG', PAGE_MARGIN_PT, PAGE_MARGIN_PT, contentWidth, heightPx / pxPerPt)
    y = end
  }

  return new File([pdf.output('blob')], filename, { type: 'application/pdf' })
}

export function isIOS() {
  return /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
}

function canShareFile(file: File) {
  return typeof navigator.share === 'function' && typeof navigator.canShare === 'function' && navigator.canShare({ files: [file] })
}

export function saveFile(file: File) {
  const url = URL.createObjectURL(file)
  const link = document.createElement('a')
  link.href = url
  link.download = file.name
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

// iOS Safari only honours share/open calls made directly inside a tap, and the tap's permission
// has expired by the time a PDF finishes rendering. Returns false when it needs a fresh tap.
export async function shareOrOpenPdf(file: File): Promise<boolean> {
  if (canShareFile(file)) {
    try {
      await navigator.share({ files: [file], title: file.name })
      return true
    } catch (err) {
      return err instanceof DOMException && err.name === 'AbortError'
    }
  }
  const opened = window.open(URL.createObjectURL(file), '_blank')
  return opened !== null
}
