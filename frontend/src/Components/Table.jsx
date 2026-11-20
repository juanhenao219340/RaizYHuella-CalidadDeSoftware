// Responsive list: a table from md up, stacked cards below md. Both use the same columns.
// A column with `srOnly: true` is hidden from the table header but still reachable by assistive tech.
function Table({ caption, columns, rows, getRowKey, isLoading = false }) {
  if (isLoading) {
    return (
      <div className="flex flex-col gap-2 rounded-lg border border-border p-4" aria-hidden="true">
        {[0, 1, 2].map((index) => (
          <div key={index} className="h-6 w-full animate-pulse rounded bg-border/40" />
        ))}
      </div>
    )
  }

  return (
    <>
      <div className="hidden overflow-x-auto rounded-lg border border-border md:block">
        <table className="w-full text-left text-sm tabular-nums">
          <caption className="sr-only">{caption}</caption>
          <thead className="bg-surface-muted">
            <tr>
              {columns.map((column) => (
                <th key={column.key} scope="col" className="px-3 py-2 font-semibold">
                  {column.srOnly ? <span className="sr-only">{column.label}</span> : column.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={getRowKey(row)} className="border-t border-border even:bg-surface-muted">
                {columns.map((column) => (
                  <td key={column.key} className="px-3 py-2 align-middle">
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul className="flex flex-col gap-3 md:hidden">
        {rows.map((row) => (
          <li key={getRowKey(row)} className="rounded-lg border border-border bg-surface-muted p-4 tabular-nums">
            {columns.map((column) => (
              <div key={column.key} className="flex items-start justify-between gap-3 py-1">
                {!column.srOnly && <span className="font-semibold">{column.label}</span>}
                <span className="text-right">{column.render(row)}</span>
              </div>
            ))}
          </li>
        ))}
      </ul>
    </>
  )
}

export default Table
