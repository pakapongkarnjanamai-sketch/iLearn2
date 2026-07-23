import {
  Activity,
  BookOpen,
  ClipboardList,
  Database,
  FileBarChart,
  FileText,
  Home,
  Library,
  Settings,
  UserCog,
  UserRound,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { NAV_LABELS, type LabelPair } from '../lib/labels'

export type NavigationItem = {
  /** Display label as a th/en pair — render with t(label). */
  label: LabelPair
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
  /** Section heading shown above the items. Omit to hide the heading. */
  label?: LabelPair
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
    items: [
      { label: NAV_LABELS.dashboard, path: '/', icon: Home },
    ],
  },
  {
    label: NAV_LABELS.learning,
    items: [
      { label: NAV_LABELS.courses, path: '/courses', icon: BookOpen },
      { label: NAV_LABELS.assignments, path: '/assignments', icon: ClipboardList },
      { label: NAV_LABELS.learnerGroups, path: '/learner-groups', icon: Users },
    ],
  },
  {
    label: NAV_LABELS.operations,
    items: [
      { label: NAV_LABELS.contentLibrary, path: '/content-library', icon: Library },
      { label: NAV_LABELS.learners, path: '/learners', icon: UserRound },
      { label: NAV_LABELS.reports, path: '/reports', icon: FileBarChart },
    ],
  },
  {
    label: NAV_LABELS.superAdmin,
    superAdminOnly: true,
    items: [
      { label: NAV_LABELS.enrollments, path: '/enrollments', icon: FileText, superAdminOnly: true },
      { label: NAV_LABELS.learningLogs, path: '/learning-logs', icon: FileText, superAdminOnly: true },
      {
        label: NAV_LABELS.masterData,
        path: '/master-data/divisions',
        icon: Database,
        superAdminOnly: true,
        children: [
          { label: NAV_LABELS.divisions, path: '/master-data/divisions', icon: Database },
          { label: NAV_LABELS.categories, path: '/master-data/categories', icon: Database },
          { label: NAV_LABELS.courseTypes, path: '/master-data/course-types', icon: Database },
          { label: NAV_LABELS.roles, path: '/master-data/roles', icon: Database },
          { label: NAV_LABELS.learnerGroupCategories, path: '/master-data/learner-group-categories', icon: Database },
        ],
      },
      { label: NAV_LABELS.adminUsers, path: '/users', icon: UserCog, superAdminOnly: true },
      { label: NAV_LABELS.systemConfig, path: '/system-config', icon: Settings, superAdminOnly: true },
      { label: NAV_LABELS.healthCheck, path: '/health-check', icon: Activity, superAdminOnly: true },
    ],
  },
]
