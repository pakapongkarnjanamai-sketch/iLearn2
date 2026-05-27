import { useCallback, useDebugValue, useSyncExternalStore } from 'react'

export function useSyncExternalStoreWithSelector<TSnapshot, TSelection>(
  subscribe: (onStoreChange: () => void) => () => void,
  getSnapshot: () => TSnapshot,
  getServerSnapshot: (() => TSnapshot) | undefined,
  selector: (snapshot: TSnapshot) => TSelection,
  isEqual?: (a: TSelection, b: TSelection) => boolean,
) {
  void isEqual

  const getSelection = useCallback(() => selector(getSnapshot()), [getSnapshot, selector])
  const getServerSelection = useCallback(
    () => (getServerSnapshot ? selector(getServerSnapshot()) : selector(getSnapshot())),
    [getSnapshot, getServerSnapshot, selector],
  )

  const value = useSyncExternalStore(subscribe, getSelection, getServerSelection)

  useDebugValue(value)
  return value
}
