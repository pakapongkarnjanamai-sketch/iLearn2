import { Check, X } from 'lucide-react'
import type { AdminGridColumn } from '../AppTable'

type TableRecord = Record<string, unknown>

type AppTableEditingRowProps<T extends TableRecord> = {
  visibleColumns: AdminGridColumn<T>[]
  values: Partial<T>
  onChange: (field: string, val: any) => void
  onSave: () => void
  onCancel: () => void
  asInputValue: (val: any) => string
}

export function AppTableEditingRow<T extends TableRecord>({
  visibleColumns,
  values,
  onChange,
  onSave,
  onCancel,
  asInputValue
}: AppTableEditingRowProps<T>) {
  return (
    <tr className="bg-indigo-50/20 border-b border-indigo-100">
      {visibleColumns.map((col) => {
        const value = values[col.dataField]
        return (
          <td key={col.dataField} className="px-4 py-2">
            {col.dataType === 'boolean' ? (
              <div className="flex justify-center">
                <input
                  type="checkbox"
                  checked={Boolean(value)}
                  onChange={(e) => onChange(col.dataField, e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-indigo-500 focus:ring-indigo-400"
                />
              </div>
            ) : col.dataType === 'number' ? (
              <input
                type="number"
                value={asInputValue(value)}
                onChange={(e) =>
                  onChange(
                    col.dataField,
                    e.target.value === '' ? '' : Number(e.target.value)
                  )
                }
                className="w-full px-2.5 py-1 border border-slate-200 rounded text-xs focus:outline-none focus:border-indigo-500"
              />
            ) : (
              <input
                type="text"
                value={asInputValue(value)}
                onChange={(e) => onChange(col.dataField, e.target.value)}
                className="w-full px-2.5 py-1 border border-slate-200 rounded text-xs focus:outline-none focus:border-indigo-500"
              />
            )}
          </td>
        )
      })}
      <td className="px-4 py-2 text-center">
        <div className="flex items-center justify-center gap-1">
          <button
            type="button"
            onClick={onSave}
            className="p-1 text-emerald-600 hover:bg-emerald-50 rounded-md transition cursor-pointer"
            title="Save New Row"
          >
            <Check className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="p-1 text-slate-400 hover:bg-slate-50 rounded-md transition cursor-pointer"
            title="Cancel Addition"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      </td>
    </tr>
  )
}
