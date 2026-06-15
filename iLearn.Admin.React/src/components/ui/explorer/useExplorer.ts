import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'

import { useBreadcrumbs } from '../../../lib/breadcrumbContext'

export type ExplorerCrumb = {
  to: string
  label: string
}

type UseExplorerOptions<TPath> = {
  rootPath: TPath
  parsePath: (params: URLSearchParams) => TPath
  toParams: (path: TPath) => Record<string, string>
  getParentPath: (path: TPath) => TPath | null
  buildBreadcrumbs: (path: TPath) => ExplorerCrumb[]
  isPathValid: (path: TPath) => boolean
  canValidatePath: boolean
}

export function useExplorer<TPath>(options: UseExplorerOptions<TPath>) {
  const {
    rootPath,
    parsePath,
    toParams,
    getParentPath,
    buildBreadcrumbs,
    isPathValid,
    canValidatePath,
  } = options

  const [searchParams, setSearchParams] = useSearchParams()
  const { setCustomCrumbs } = useBreadcrumbs()

  const [searchTerm, setSearchTerm] = useState('')

  const searchParamsKey = searchParams.toString()
  const path = useMemo(() => {
    return parsePath(new URLSearchParams(searchParamsKey))
  }, [parsePath, searchParamsKey])

  const crumbs = useMemo(() => buildBreadcrumbs(path), [buildBreadcrumbs, path])
  const crumbsKey = useMemo(() => {
    return crumbs.map(crumb => `${crumb.to}|${crumb.label}`).join('||')
  }, [crumbs])
  const lastCrumbsKey = useRef<string>('')

  const navigateToPath = useCallback((nextPath: TPath, replace = false) => {
    const params = toParams(nextPath)
    setSearchParams(params, { replace })
    setSearchTerm('')
  }, [setSearchParams, toParams])

  const goBack = useCallback(() => {
    const parentPath = getParentPath(path)
    if (!parentPath) return

    navigateToPath(parentPath)
  }, [getParentPath, navigateToPath, path])

  useEffect(() => {
    if (!canValidatePath) return

    if (!isPathValid(path)) {
      navigateToPath(rootPath, true)
    }
  }, [canValidatePath, isPathValid, navigateToPath, path, rootPath])

  useEffect(() => {
    if (lastCrumbsKey.current === crumbsKey) return

    lastCrumbsKey.current = crumbsKey
    setCustomCrumbs(crumbs)
  }, [crumbs, crumbsKey, setCustomCrumbs])

  useEffect(() => {
    return () => {
      setCustomCrumbs(null)
    }
  }, [setCustomCrumbs])

  const filterBySearch = useCallback(<TItem,>(
    items: TItem[],
    predicate: (item: TItem, normalizedTerm: string) => boolean
  ) => {
    const normalizedTerm = searchTerm.trim().toLowerCase()
    if (!normalizedTerm) return items

    return items.filter(item => predicate(item, normalizedTerm))
  }, [searchTerm])

  return {
    path,
    searchTerm,
    setSearchTerm,
    navigateToPath,
    goBack,
    filterBySearch,
  }
}
