/**
 * Utility to export headers and rows of data into a downloadable CSV file.
 * Handles escaping and embeds a UTF-8 BOM so Excel opens Thai characters correctly.
 */
export function exportRowsAsCsv(
  filename: string,
  header: string[],
  rows: (string | number | null | undefined)[][]
): void {
  const csv = [header, ...rows]
    .map((r) =>
      r
        .map((v) => {
          const str = v === null || v === undefined ? '' : String(v)
          return `"${str.replace(/"/g, '""')}"`
        })
        .join(',')
    )
    .join('\r\n')

  // The first blob part is the U+FEFF BOM char — required so Excel decodes Thai names as UTF-8
  const blob = new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}
