import { useState } from 'react'
import { ChevronDown, ChevronRight, Folder, FolderOpen, Layers, Shield } from 'lucide-react'

export type TreeViewNode = {
  id: string
  text: string
  items?: TreeViewNode[]
  isDivision?: boolean
  isRoot?: boolean
  divisionId?: number
  categoryId?: number
}

type AppTreeViewProps = {
  items: TreeViewNode[]
  onItemClick: (event: { itemData: TreeViewNode }) => void
}

export function AppTreeView({ items, onItemClick }: AppTreeViewProps) {
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const handleNodeClick = (node: TreeViewNode) => {
    setSelectedId(node.id)
    onItemClick({ itemData: node })
  }

  return (
    <div className="text-xs select-none">
      <ul className="space-y-1">
        {items.map(item => (
          <TreeViewItem
            key={item.id}
            node={item}
            selectedId={selectedId}
            onSelect={handleNodeClick}
            level={0}
          />
        ))}
      </ul>
    </div>
  )
}

type TreeViewItemProps = {
  node: TreeViewNode
  selectedId: string | null
  onSelect: (node: TreeViewNode) => void
  level: number
}

function TreeViewItem({ node, selectedId, onSelect, level }: TreeViewItemProps) {
  const hasChildren = Array.isArray(node.items) && node.items.length > 0
  const [expanded, setExpanded] = useState(level === 0 || node.isRoot || false) // auto-expand root/top level

  const isSelected = selectedId === node.id

  const handleToggle = (e: React.MouseEvent) => {
    e.stopPropagation()
    setExpanded(prev => !prev)
  }

  const handleRowClick = () => {
    onSelect(node)
  }

  // Choose dynamic premium icon based on node specifications
  const getIcon = () => {
    if (node.isRoot) return <Layers className="h-4 w-4 text-indigo-500 shrink-0" />
    if (node.isDivision) return <Shield className="h-3.5 w-3.5 text-purple-500 shrink-0" />
    return hasChildren 
      ? (expanded ? <FolderOpen className="h-3.5 w-3.5 text-amber-500 shrink-0" /> : <Folder className="h-3.5 w-3.5 text-amber-500 shrink-0" />)
      : <Folder className="h-3.5 w-3.5 text-slate-400 shrink-0" />
  }

  return (
    <li className="space-y-0.5">
      <div
        onClick={handleRowClick}
        style={{ paddingLeft: `${level * 16 + 8}px` }}
        className={`flex items-center gap-2 py-1.5 pr-2 rounded-lg cursor-pointer transition-all duration-150 relative overflow-hidden group ${
          isSelected
            ? 'bg-indigo-50/70 text-blue-700 font-bold border-l-3 border-l-blue-600 pl-[5px]'
            : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
        }`}
      >
        {/* Toggle chevron */}
        {hasChildren ? (
          <button
            onClick={handleToggle}
            className="p-0.5 hover:bg-slate-200/50 rounded-md transition text-slate-400 hover:text-slate-600 shrink-0 cursor-pointer"
          >
            {expanded ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
          </button>
        ) : (
          <span className="w-4.5 shrink-0" /> /* spacer */
        )}

        {/* Node Icon */}
        {getIcon()}

        {/* Node Label */}
        <span className="truncate">{node.text}</span>
      </div>

      {/* Child Nodes */}
      {hasChildren && expanded && (
        <ul className="mt-0.5 space-y-1">
          {node.items!.map(child => (
            <TreeViewItem
              key={child.id}
              node={child}
              selectedId={selectedId}
              onSelect={onSelect}
              level={level + 1}
            />
          ))}
        </ul>
      )}
    </li>
  )
}
