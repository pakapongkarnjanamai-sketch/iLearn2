import {
  BookOpen,
  ClipboardList,
  Database,
  Home,
  Library,
  ShieldCheck,
  UserRound,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

export type NavigationItem = {
  label: string
  path: string
  icon: LucideIcon
}

export const navigationItems: NavigationItem[] = [
  { label: 'Dashboard', path: '/', icon: Home },
  { label: 'Courses', path: '/courses', icon: BookOpen },
  { label: 'Content Library', path: '/content-library', icon: Library },
  { label: 'Assignments', path: '/assignments', icon: ClipboardList },
  { label: 'Student Groups', path: '/learner-groups', icon: Users },
  { label: 'Learners', path: '/learners', icon: UserRound },
  { label: 'Master Data', path: '/master-data', icon: Database },
  { label: 'Access Control', path: '/access-denied', icon: ShieldCheck },
]