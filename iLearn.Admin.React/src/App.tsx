import { Fragment, type ReactNode } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { RequireRole } from './components/auth/RequireRole'
import { AccessDeniedPage } from './pages/AccessDeniedPage'
import { DashboardPage } from './pages/DashboardPage'
import { EntityListPage } from './pages/EntityListPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { adminListConfigs } from './pages/moduleConfigs'
import { SystemConfigPage } from './pages/system-config/SystemConfigPage'
import { HealthCheckPage } from './pages/system-config/HealthCheckPage'
import { CourseListPage } from './pages/courses/CourseListPage'
import { CourseDetailPage } from './pages/courses/CourseDetailPage'
import { CourseEditorPage } from './pages/courses/CourseEditorPage'
import { VersionDetailPage } from './pages/courses/VersionDetailPage'
import { VersionFormPage } from './pages/courses/VersionFormPage'
import { LearnerGroupListPage } from './pages/learner-groups/LearnerGroupListPage'
import { LearnerGroupDetailPage } from './pages/learner-groups/LearnerGroupDetailPage'
import { LearnerGroupEditorPage } from './pages/learner-groups/LearnerGroupEditorPage'
import { LearnerListPage } from './pages/learners/LearnerListPage'
import { LearnerProfilePage } from './pages/learners/LearnerProfilePage'
import { AssignmentDetailPage } from './pages/assignments/AssignmentDetailPage'
import { AssignmentReportPage } from './pages/assignments/AssignmentReportPage'
import { AssignmentGanttPage } from './pages/assignments/AssignmentGanttPage'
import { BulkAssignPage } from './pages/assignments/BulkAssignPage'
import { ContentItemDetailPage } from './pages/content-library/ContentItemDetailPage'
import { ContentItemEditorPage } from './pages/content-library/ContentItemEditorPage'
import { LearnerGroupCategoriesPage } from './pages/master-data/LearnerGroupCategoriesPage'
import { LearnerGroupCategoryEditorPage } from './pages/master-data/LearnerGroupCategoryEditorPage'
import { MasterDataDetailPage } from './pages/master-data/MasterDataDetailPage'
import { AdminUsersPage } from './pages/users/AdminUsersPage'
import { UserEditorPage } from './pages/users/UserEditorPage'
import { UserDetailPage } from './pages/users/UserDetailPage'
import { ReportHubPage } from './pages/reports/ReportHubPage'
import { ComplianceReportPage } from './pages/reports/ComplianceReportPage'
import { TranscriptReportPage } from './pages/reports/TranscriptReportPage'
import { CourseSummaryReportPage } from './pages/reports/CourseSummaryReportPage'
import { ActivityReportPage } from './pages/reports/ActivityReportPage'
import { AssignmentSummaryReportPage } from './pages/reports/AssignmentSummaryReportPage'
import { NotificationsPage } from './pages/notifications/NotificationsPage'

function LegacyStudentGroupsRedirect() {
  const location = useLocation()
  const nextPath = location.pathname.replace(/^\/student-groups/, '/learner-groups')
  return <Navigate to={`${nextPath}${location.search}${location.hash}`} replace />
}

/*
 * React Router reuses a component instance when two routes render the same
 * component type (e.g. courses/new ↔ courses/:id/edit, or /courses/1 → /courses/2),
 * so internal state — form values, active tab, selections — leaks across pages.
 * Keying by pathname forces a clean remount whenever the path changes.
 * Wrap every detail/editor route element with this.
 */
function Remount({ children }: { children: ReactNode }) {
  const { pathname } = useLocation()
  return <Fragment key={pathname}>{children}</Fragment>
}

export function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<DashboardPage />} />

        {/* Courses */}
        <Route path="courses" element={<CourseListPage />} />
        <Route path="courses/new" element={<Remount><CourseEditorPage /></Remount>} />
        <Route path="courses/:id" element={<Remount><CourseDetailPage /></Remount>} />
        <Route path="courses/:id/edit" element={<Remount><CourseEditorPage /></Remount>} />
        <Route path="courses/:courseId/version/new" element={<Remount><VersionFormPage /></Remount>} />
        <Route path="courses/:courseId/version/:versionId" element={<Remount><VersionDetailPage /></Remount>} />
        <Route path="courses/:courseId/version/:id/edit" element={<Remount><VersionFormPage /></Remount>} />

        {/* Content Library */}
        <Route path="content-library" element={<EntityListPage config={adminListConfigs.contentLibrary} />} />
        <Route
          path="content-library/new"
          element={
            <RequireRole superAdminOnly>
              <Remount><ContentItemEditorPage /></Remount>
            </RequireRole>
          }
        />
        <Route path="content-library/:id" element={<Remount><ContentItemDetailPage /></Remount>} />
        <Route
          path="content-library/:id/edit"
          element={
            <RequireRole superAdminOnly>
              <Remount><ContentItemEditorPage /></Remount>
            </RequireRole>
          }
        />

        {/* Assignments */}
        <Route path="assignments" element={<EntityListPage config={adminListConfigs.assignments} />} />
        <Route path="assignments/gantt" element={<AssignmentGanttPage />} />
        <Route path="assignments/bulk" element={<BulkAssignPage />} />
        <Route path="assignments/:id" element={<Remount><AssignmentDetailPage /></Remount>} />
        <Route path="assignments/:id/report" element={<Remount><AssignmentReportPage /></Remount>} />

        {/* Learner Groups */}
        <Route path="learner-groups" element={<LearnerGroupListPage />} />
        <Route path="learner-groups/new" element={<Remount><LearnerGroupEditorPage /></Remount>} />
        <Route path="learner-groups/:id" element={<Remount><LearnerGroupDetailPage /></Remount>} />
        <Route path="student-groups/*" element={<LegacyStudentGroupsRedirect />} />

        {/* Learners */}
        <Route path="learners" element={<LearnerListPage />} />
        <Route path="learners/:id/profile" element={<Remount><LearnerProfilePage /></Remount>} />

        {/* Notifications (full page — entry via bell footer, no sidebar item) */}
        <Route path="notifications" element={<Remount><NotificationsPage /></Remount>} />

        {/* Reports */}
        <Route path="reports" element={<Remount><ReportHubPage /></Remount>} />
        <Route path="reports/compliance" element={<Remount><ComplianceReportPage /></Remount>} />
        <Route path="reports/transcript" element={<Remount><TranscriptReportPage /></Remount>} />
        <Route path="reports/courses" element={<Remount><CourseSummaryReportPage /></Remount>} />
        <Route path="reports/assignments" element={<Remount><AssignmentSummaryReportPage /></Remount>} />
        <Route path="reports/activity" element={<Remount><ActivityReportPage /></Remount>} />

        {/* Operations */}
        <Route
          path="learning-logs"
          element={
            <RequireRole superAdminOnly>
              <EntityListPage config={adminListConfigs.learningLogs} />
            </RequireRole>
          }
        />
        <Route
          path="enrollments"
          element={
            <RequireRole superAdminOnly>
              <EntityListPage config={adminListConfigs.enrollments} />
            </RequireRole>
          }
        />

        {/* Admin Users */}
        <Route
          path="users"
          element={
            <RequireRole superAdminOnly>
              <AdminUsersPage />
            </RequireRole>
          }
        />
        <Route
          path="users/new"
          element={
            <RequireRole superAdminOnly>
              <Remount>
                <UserEditorPage />
              </Remount>
            </RequireRole>
          }
        />
        <Route
          path="users/:id"
          element={
            <RequireRole superAdminOnly>
              <Remount>
                <UserDetailPage />
              </Remount>
            </RequireRole>
          }
        />
        <Route
          path="users/:id/edit"
          element={
            <RequireRole superAdminOnly>
              <Remount>
                <UserEditorPage />
              </Remount>
            </RequireRole>
          }
        />

        {/* Master Data — SuperAdmin only */}
        <Route
          path="master-data"
          element={<Navigate to="/master-data/divisions" replace />}
        />
        <Route
          path="master-data/divisions"
          element={
            <RequireRole superAdminOnly>
              <EntityListPage config={adminListConfigs.masterDataDivisions} />
            </RequireRole>
          }
        />
        <Route
          path="master-data/categories"
          element={
            <RequireRole superAdminOnly>
              <EntityListPage config={adminListConfigs.masterDataCategories} />
            </RequireRole>
          }
        />
        <Route
          path="master-data/course-types"
          element={
            <RequireRole superAdminOnly>
              <EntityListPage config={adminListConfigs.masterDataCourseTypes} />
            </RequireRole>
          }
        />
        <Route
          path="master-data/roles"
          element={
            <RequireRole superAdminOnly>
              <EntityListPage config={adminListConfigs.masterDataRoles} />
            </RequireRole>
          }
        />
        <Route
          path="master-data/learner-group-categories"
          element={
            <RequireRole superAdminOnly>
              <LearnerGroupCategoriesPage />
            </RequireRole>
          }
        />
        <Route
          path="master-data/learner-group-categories/new"
          element={
            <RequireRole superAdminOnly>
              <Remount><LearnerGroupCategoryEditorPage /></Remount>
            </RequireRole>
          }
        />
        <Route
          path="master-data/learner-group-categories/:id/edit"
          element={
            <RequireRole superAdminOnly>
              <Remount><LearnerGroupCategoryEditorPage /></Remount>
            </RequireRole>
          }
        />
        <Route
          path="master-data/student-group-categories"
          element={<Navigate to="/master-data/learner-group-categories" replace />}
        />
        <Route
          path="master-data/:type/new"
          element={
            <RequireRole superAdminOnly>
              <Remount><MasterDataDetailPage isNew={true} /></Remount>
            </RequireRole>
          }
        />
        <Route
          path="master-data/:type/:id"
          element={
            <RequireRole superAdminOnly>
              <Remount><MasterDataDetailPage /></Remount>
            </RequireRole>
          }
        />

        <Route
          path="system-config"
          element={
            <RequireRole superAdminOnly>
              <SystemConfigPage />
            </RequireRole>
          }
        />
        <Route
          path="health-check"
          element={
            <RequireRole superAdminOnly>
              <HealthCheckPage />
            </RequireRole>
          }
        />
        <Route path="access-denied" element={<AccessDeniedPage />} />
        <Route path="not-found" element={<NotFoundPage />} />
        <Route path="*" element={<Navigate to="/not-found" replace />} />
      </Route>
    </Routes>
  )
}
