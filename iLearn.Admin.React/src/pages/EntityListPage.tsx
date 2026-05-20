import { useMemo, useRef } from 'react'
import DataGrid, {
  Column,
  FilterRow,
  HeaderFilter,
  Pager,
  Paging,
  Scrolling,
  SearchPanel,
  Sorting,
  type DataGridRef,
} from 'devextreme-react/data-grid'
import type { DataErrorOccurredEvent } from 'devextreme/ui/data_grid'
import { RefreshCw } from 'lucide-react'
import { AppButton } from '../components/ui/AppButton'
import { DataGridSurface } from '../components/ui/DataGridSurface'
import { PageHeader } from '../components/ui/PageHeader'
import { createAdminDataSource } from '../lib/createDataSource'
import { toast } from '../lib/toast'
import type { AdminListConfig } from './moduleConfigs'

type EntityListPageProps = {
  config: AdminListConfig
}

export function EntityListPage({ config }: EntityListPageProps) {
  const gridRef = useRef<DataGridRef>(null)
  const dataSource = useMemo(
    () => createAdminDataSource({ controller: config.controller, key: config.key }),
    [config.controller, config.key],
  )

  const refreshGrid = () => {
    gridRef.current?.instance().refresh()
  }

  const handleDataError = (event: DataErrorOccurredEvent) => {
    const message = event.error?.message || 'Unable to load grid data'
    toast.error(message)
  }

  return (
    <>
      <PageHeader
        title={config.title}
        eyebrow={config.eyebrow}
        description={config.description}
        actions={
          <AppButton variant="secondary" icon={RefreshCw} onClick={refreshGrid}>
            Refresh
          </AppButton>
        }
      />

      <DataGridSurface title={config.gridTitle} note={config.gridNote}>
        <DataGrid
          ref={gridRef}
          dataSource={dataSource}
          remoteOperations
          allowColumnReordering
          allowColumnResizing
          columnAutoWidth
          columnResizingMode="widget"
          focusedRowEnabled
          height="100%"
          repaintChangesOnly
          showBorders={false}
          showColumnLines={false}
          showRowLines
          wordWrapEnabled={false}
          onDataErrorOccurred={handleDataError}
        >
          <SearchPanel visible width={280} placeholder="Search" />
          <FilterRow visible applyFilter="auto" />
          <HeaderFilter visible />
          <Sorting mode="multiple" />
          <Scrolling mode="virtual" rowRenderingMode="virtual" />
          <Paging defaultPageSize={50} />
          <Pager visible showInfo showNavigationButtons allowedPageSizes={[25, 50, 100]} showPageSizeSelector />

          {config.columns.map((column) => (
            <Column key={column.dataField} {...column} />
          ))}
        </DataGrid>
      </DataGridSurface>
    </>
  )
}