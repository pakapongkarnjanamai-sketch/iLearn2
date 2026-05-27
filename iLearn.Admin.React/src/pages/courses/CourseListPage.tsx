import { useMemo, useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Loader2, Plus, Layers, Sliders } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppTable, type AdminGridColumn } from '../../components/ui/AppTable'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { createAdminDataSource } from '../../lib/createDataSource'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

type CourseTypeLookup = {
  id: number
  name: string
}

type CourseFilterChip = {
  key: string
  courseTypeId: number | null
  label: string
}

type CourseGridRow = Record<string, unknown> & {
  id?: number
  title?: string
  code?: string
  courseTypeName?: string
  statusName?: string
  isActive?: boolean
  categoryName?: string
}

export function CourseListPage() {
  const navigate = useNavigate()
  
  const [treeData, setTreeData] = useState<TreeViewNode[]>([])
  const [loadingTree, setLoadingTree] = useState(true)
  
  const [courseTypes, setCourseTypes] = useState<CourseFilterChip[]>([
    { key: 'all', courseTypeId: null, label: 'All Types' }
  ])
  const [selectedTypeKey, setSelectedTypeKey] = useState('all')
  const [selectedTreeNode, setSelectedTreeNode] = useState<TreeViewNode | null>(null)
  
  // Declarative query filter state
  const [gridFilters, setGridFilters] = useState<any[]>([])

  // Memoized ASP.NET API Query client store
  const store = useMemo(() => {
    return createAdminDataSource<CourseGridRow>({ 
      controller: 'CoursesCRUD', 
      key: 'id',
      enableCrud: true 
    })
  }, [])

  // Load Categories & Divisions TreeView
  const loadTreeData = async () => {
    setLoadingTree(true)
    try {
      const data = await fetchWithAccessControl<TreeViewNode[]>('Divisions/GetTree')
      setTreeData(data)
    } catch (err) {
      console.error('Failed to load category tree', err)
      toast.error('Unable to load category hierarchy')
    } finally {
      setLoadingTree(false)
    }
  }

  // Load Course Types for top chips filter
  const loadCourseTypes = async () => {
    try {
      const data = await fetchWithAccessControl<CourseTypeLookup[]>('Courses/course-types-lookup')
      if (Array.isArray(data) && data.length > 0) {
        const chips = [
          { key: 'all', courseTypeId: null, label: 'All Types' },
          ...data.map(type => ({
            key: `type-${type.id}`,
            courseTypeId: type.id,
            label: type.name
          }))
        ]
        setCourseTypes(chips)
      }
    } catch (err) {
      console.error('Failed to load course types lookup', err)
    }
  }

  useEffect(() => {
    loadTreeData()
    loadCourseTypes()
  }, [])

  // Combine and Apply active filters declaratively
  const applyGridFilters = (treeNode: TreeViewNode | null, chipKey: string) => {
    const conditions: any[] = []

    // 1. Division or Category Filter from left tree
    if (treeNode && !treeNode.isRoot) {
      if (treeNode.categoryId) {
        conditions.push(['categoryId', '=', treeNode.categoryId])
      } else if (treeNode.divisionId) {
        conditions.push(['divisionId', '=', treeNode.divisionId])
      }
    }

    // 2. Course Type chip filter
    const selectedType = courseTypes.find(t => t.key === chipKey)
    if (selectedType && selectedType.courseTypeId !== null) {
      if (conditions.length > 0) {
        conditions.push('and')
      }
      conditions.push(['courseTypeId', '=', selectedType.courseTypeId])
    }

    setGridFilters(conditions)
  }

  // Handle Tree View click filtering
  const handleTreeSelection = (e: { itemData: TreeViewNode }) => {
    const node = e.itemData
    setSelectedTreeNode(node)
    applyGridFilters(node, selectedTypeKey)
  }

  // Handle Quick chip filtering
  const handleChipSelect = (key: string) => {
    setSelectedTypeKey(key)
    applyGridFilters(selectedTreeNode, key)
  }

  // Row selection/routing logic
  const handleRowDoubleClick = (e: { data: CourseGridRow }) => {
    if (e.data?.id) {
      navigate(`/courses/${e.data.id}`)
    }
  }

  // Table Columns config with custom renders
  const gridColumns = useMemo<AdminGridColumn<CourseGridRow>[]>(() => [
    { 
      dataField: 'title', 
      caption: 'Course Identity', 
      minWidth: 280,
      cellRender: ({ data }) => (
        <div className="flex flex-col py-0.5 text-left">
          <span className="text-slate-800 font-bold text-sm leading-tight">{data.title}</span>
          <span className="text-slate-400 font-mono text-xxs mt-0.5">{data.code}</span>
        </div>
      )
    },
    { 
      dataField: 'courseTypeName', 
      caption: 'Course Type', 
      width: 130,
      alignment: 'center',
      cellRender: ({ value }) => {
        const type = String(value || '—')
        const isSpecial = type.toLowerCase().includes('special')
        return (
          <span className={`inline-flex items-center px-2 py-0.5 rounded text-xxs font-bold ${
            isSpecial 
              ? 'bg-purple-100 text-purple-800 border border-purple-200/50' 
              : 'bg-blue-100 text-blue-800 border border-blue-200/50'
          }`}>
            {type}
          </span>
        )
      }
    },
    { 
      dataField: 'statusName', 
      caption: 'Status', 
      width: 110,
      alignment: 'center',
      cellRender: ({ value, data }) => {
        const status = String(value || (data.isActive ? 'Open' : 'Closed'))
        const isDraft = status.toLowerCase() === 'draft'
        const isRetired = status.toLowerCase() === 'retired'
        const isOpen = status.toLowerCase() === 'open' || status.toLowerCase() === 'active'
        
        let toneClass = 'bg-slate-100 text-slate-800 border-slate-200'
        if (isOpen) toneClass = 'bg-emerald-100 text-emerald-800 border-emerald-200 font-bold'
        else if (isDraft) toneClass = 'bg-amber-100 text-amber-800 border-amber-200'
        else if (isRetired) toneClass = 'bg-rose-100 text-rose-800 border-rose-200'

        return (
          <span className={`inline-flex items-center px-2 py-0.5 rounded text-xxs font-semibold border ${toneClass}`}>
            {status}
          </span>
        )
      }
    },
    { 
      dataField: 'categoryName', 
      caption: 'Category / Department', 
      minWidth: 160,
      cellRender: ({ value }) => (
        <span className="text-slate-500 font-medium text-xs text-left block">{String(value || '—')}</span>
      )
    }
  ], [])

  const actionButtons = useMemo(() => [{
    hint: 'Open Course Details',
    icon: 'info',
    onClick: (e: { row: { data: CourseGridRow } }) => {
      if (e.row?.data?.id) {
        navigate(`/courses/${e.row.data.id}`)
      }
    }
  }], [navigate])

  return (
    <>
      <div className="grid min-h-0 flex-1 grid-cols-1 items-stretch gap-5 md:grid-cols-4">
        
        {/* Categories Tree Column */}
        <aside className="admin-card flex min-h-0 flex-col p-4 md:col-span-1">
          <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-3 select-none">
            <Layers className="h-4 w-4 text-slate-600" />
            <h2 className="text-sm font-bold text-slate-700">Course Categories</h2>
          </div>
          
          <div className="custom-scrollbar min-h-0 flex-1 overflow-y-auto">
            {loadingTree ? (
              <div className="flex h-40 items-center justify-center">
                <Loader2 className="h-5 w-5 animate-spin text-slate-400" />
              </div>
            ) : (
              <AppTreeView
                items={treeData}
                onItemClick={handleTreeSelection}
              />
            )}
          </div>
        </aside>

        {/* Custom DataGrid Replacement Column */}
        <main className="flex min-h-0 flex-col md:col-span-3">
          <DataGridSurface 
            title={selectedTreeNode ? `Filter: ${selectedTreeNode.text}` : 'Course Directory'} 
            note="Double-click any row to view complete details, manage versions, and preview training files."
            actions={
              <Link to="/courses/new">
                <AppButton variant="primary" icon={Plus}>
                  Create Course
                </AppButton>
              </Link>
            }
          >
            <AppTable
              store={store}
              columns={gridColumns}
              noDataText="No courses match the active query parameters"
              onRowDblClick={handleRowDoubleClick}
              searchPlaceholder="Search by course name or code..."
              searchExpr={['title', 'code']}
              externalFilters={gridFilters}
              toolbarContent={
                <>
                  <div className="flex items-center gap-1.5 text-xs font-bold uppercase text-slate-500">
                    <Sliders className="h-4 w-4 text-slate-500" aria-hidden="true" />
                    <span>Course Type</span>
                  </div>
                  {courseTypes.map(chip => (
                    <button
                      key={chip.key}
                      type="button"
                      onClick={() => handleChipSelect(chip.key)}
                      className={`rounded-md border px-3 py-1.5 text-xs font-semibold transition-colors ${
                        selectedTypeKey === chip.key
                          ? 'border-blue-600 bg-blue-600 text-white'
                          : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                      }`}
                    >
                      {chip.label}
                    </button>
                  ))}
                </>
              }
              actionButtons={actionButtons}
            />
          </DataGridSurface>
        </main>

      </div>
    </>
  )
}
