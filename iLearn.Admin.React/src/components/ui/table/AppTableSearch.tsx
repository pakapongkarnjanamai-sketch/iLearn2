import type { ReactNode } from 'react'
import { ListToolbar } from '../ListToolbar'

type AppTableSearchProps = {
  value: string
  onChange: (value: string) => void
  totalCount: number
  placeholder?: string
  toolbarContent?: ReactNode
}

export function AppTableSearch({
  value,
  onChange,
  totalCount,
  placeholder = 'Search...',
  toolbarContent
}: AppTableSearchProps) {
  return (
    <ListToolbar
      count={totalCount}
      countUnit="records"
      searchValue={value}
      onSearchChange={onChange}
      searchPlaceholder={placeholder}
      toolbarContent={toolbarContent}
    />
  )
}
