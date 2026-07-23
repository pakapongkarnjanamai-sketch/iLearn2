import type { ReactNode } from 'react'
import { ListToolbar } from '../ListToolbar'

type AppTableSearchProps = {
  value: string
  onChange: (value: string) => void
  totalCount: number
  placeholder?: string | undefined
  toolbarContent?: ReactNode
}

export function AppTableSearch({
  value,
  onChange,
  totalCount,
  placeholder,
  toolbarContent
}: AppTableSearchProps) {
  // countUnit/searchPlaceholder defaults come from ListToolbar (central dictionary).
  return (
    <ListToolbar
      count={totalCount}
      searchValue={value}
      onSearchChange={onChange}
      {...(placeholder !== undefined ? { searchPlaceholder: placeholder } : {})}
      toolbarContent={toolbarContent}
    />
  )
}
