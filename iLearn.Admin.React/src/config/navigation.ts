import {
  BookOpen,
  ClipboardList,
  Database,
  FileText,
  Home,
  Library,
  Settings,
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

export type NavigationSection = {
  /** Section heading shown above the items. Empty string hides the heading. */
  label: string
  /** Hides the whole section (heading included) from non-SuperAdmin users. */
  superAdminOnly?: boolean
  items: NavigationItem[]
}

/*
 * Sidebar is split into sections by audience:
 *   - unlabeled top section + "Learning" + "Operations" = everyday admin work
 *   - "Super Admin" = privileged configuration, hidden entirely for regular admins
 * Keep superAdminOnly items inside the Super Admin section so the separation
 * stays visible in the UI, not just in the role filter.
 */
export const navigationSections: NavigationSection[] = [
  {
    label: '',
    items: [
      { label: 'Dashboard', path: '/', icon: Home },
    ],
  },
  {
    label: 'Learning',
    items: [
      { label: 'Courses', path: '/courses', icon: BookOpen },
      { label: 'Content Library', path: '/content-library', icon: Library },
      { label: 'Assignments', path: '/assignments', icon: ClipboardList },
      { label: 'Learner Groups', path: '/learner-groups', icon: Users },
      { label: 'Learners', path: '/learners', icon: UserRound },
    ],
  },
  {
    label: 'Operations',
    items: [
      { label: 'Learning Logs', path: '/learning-logs', icon: FileText },
    ],
  },
  {
    label: 'Super Admin',
    superAdminOnly: true,
    items: [
      { label: 'Enrollments', path: '/enrollments', icon: FileText, superAdminOnly: true },
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
          { label: 'Learner Group Categories', path: '/master-data/learner-group-categories', icon: Database },
        ],
      },
      { label: 'Admin Users', path: '/users', icon: UserCog, superAdminOnly: true },
      { label: 'System Config', path: '/system-config', icon: Settings, superAdminOnly: true },
    ],
  },
]
