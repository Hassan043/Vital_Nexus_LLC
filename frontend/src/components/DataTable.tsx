import type { ReactNode } from 'react'

type TableSectionProps = {
  title: string
  description?: string
  children: ReactNode
}

export function TableSection({ title, description, children }: TableSectionProps) {
  return (
    <section className="table-section" aria-labelledby={title.replace(/\s+/g, '-')}>
      <div className="table-section-header">
        <h2 id={title.replace(/\s+/g, '-')}>{title}</h2>
        {description ? <p className="table-section-desc">{description}</p> : null}
      </div>
      {children}
    </section>
  )
}

type KeyValueRow = {
  label: string
  value: ReactNode
}

type KeyValueTableProps = {
  rows: KeyValueRow[]
  caption?: string
}

export function KeyValueTable({ rows, caption }: KeyValueTableProps) {
  return (
    <table className="data-table data-table-kv">
      {caption ? <caption className="sr-only">{caption}</caption> : null}
      <tbody>
        {rows.map((row) => (
          <tr key={row.label}>
            <th scope="row">{row.label}</th>
            <td>{row.value}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export type DataTableColumn<T> = {
  key: string
  header: string
  render: (row: T) => ReactNode
  align?: 'left' | 'center' | 'right'
}

type DataTableProps<T> = {
  columns: DataTableColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  caption?: string
  emptyMessage?: string
}

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  caption,
  emptyMessage = 'No rows.',
}: DataTableProps<T>) {
  return (
    <table className="data-table">
      {caption ? <caption className="sr-only">{caption}</caption> : null}
      <thead>
        <tr>
          {columns.map((column) => (
            <th key={column.key} className={column.align ? `align-${column.align}` : undefined}>
              {column.header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.length === 0 ? (
          <tr>
            <td colSpan={columns.length} className="data-table-empty">
              {emptyMessage}
            </td>
          </tr>
        ) : (
          rows.map((row) => (
            <tr key={rowKey(row)}>
              {columns.map((column) => (
                <td key={column.key} className={column.align ? `align-${column.align}` : undefined}>
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))
        )}
      </tbody>
    </table>
  )
}

type StatusCellProps = {
  complete: boolean
  label?: string
}

export function StatusCell({ complete, label }: StatusCellProps) {
  return (
    <span className={`status-cell ${complete ? 'status-complete' : 'status-pending'}`}>
      {complete ? 'Complete' : 'Pending'}
      {label ? <span className="status-cell-label"> — {label}</span> : null}
    </span>
  )
}
