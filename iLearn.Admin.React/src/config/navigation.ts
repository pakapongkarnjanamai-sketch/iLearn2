import {
  BookOpen,
  ClipboardList,
  Database,
  FileText,
  Home,
  Layers,
  Library,
  Settings,
  ShieldCheck,
  UserCog,
  UserRound,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

export type NavigationItem = {
  label: string
  path: string
  icon: LucideIcon
  /** When set, only users in any of these roles (or SuperAdmin) can see this item. */
  requiredRoles?: readonly string[]
  /** Shortcut for SuperAdmin-only items. */
  superAdminOnly?: boolean
  /** Optional nested items rendered under the parent. */
  children?: NavigationItem[]
}

export const navigationItems: NavigationItem[] = [
  { label: 'Dashboard', path: '/', icon: Home },
  { label: 'Courses', path: '/courses', icon: BookOpen },
  { label: 'Content Library', path: '/content-library', icon: Library },
  {
    label: 'Assignments',
    path: '/assignments',
    icon: ClipboardList,
    children: [
      { label: 'Batches', path: '/assignments', icon: ClipboardList },
      { label: 'Schedule (Gantt)', path: '/assignments/gantt', icon: Layers },
      { label: 'Bulk Assign', path: '/assignments/bulk', icon: ClipboardList },
    ],
  },
  { label: 'Student Groups', path: '/student-groups', icon: Users },
  { label: 'Learners', path: '/learners', icon: UserRound },
  {
    label: 'Operations',
    path: '/learning-logs',
    icon: FileText,
    children: [
      { label: 'Learning Logs', path: '/learning-logs', icon: FileText },
      { label: 'Enrollments', path: '/enrollments', icon: FileText, superAdminOnly: true },
    ],
  },
  {
    label: 'Master Data',
    path: '/master-data/divisions',
    icon: Database,
    superAdminOnly: true,
    children: [
      { label: 'Divisions', path: '/master-data/divisions', icon: Database },
      { label: 'Categories', path: '/master-data/categories', icon: Database },
      { label: 'Course Types', path: '/master-data/course-types', icon: Database },
      { label: 'Roles', path: '/master-data/roles', icon: Database },
      { label: 'Student Group Categories', path: '/master-data/student-group-categories', icon: Database },
    ],
  },
  { label: 'Admin Users', path: '/users', icon: UserCog, superAdminOnly: true },
  { label: 'System Config', path: '/system-config', icon: Settings, superAdminOnly: true },
  { label: 'Access Control', path: '/access-denied', icon: ShieldCheck, superAdminOnly: true },
]
