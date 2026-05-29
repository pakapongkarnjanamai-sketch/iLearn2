import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { RequireRole } from './components/auth/RequireRole'
import { AccessDeniedPage } from './pages/AccessDeniedPage'
import { DashboardPage } from './pages/DashboardPage'
import { EntityListPage } from './pages/EntityListPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { adminListConfigs } from './pages/moduleConfigs'
import { SystemConfigPage } from './pages/system-config/SystemConfigPage'
import { CourseListPage } from './pages/courses/CourseListPage'
import { CourseDetailPage } from './pages/courses/CourseDetailPage'
import { CourseEditorPage } from './pages/courses/CourseEditorPage'
import { VersionFormPage } from './pages/courses/VersionFormPage'
import { LearnerGroupDetailPage } from './pages/learner-groups/LearnerGroupDetailPage'
import { LearnerGroupEditorPage } from './pages/learner-groups/LearnerGroupEditorPage'
import { LearnerProfilePage } from './pages/learners/LearnerProfilePage'
import { AssignmentDetailPage } from './pages/assignments/AssignmentDetailPage'
import { AssignmentReportPage } from './pages/assignments/AssignmentReportPage'
import { AssignmentGanttPage } from './pages/assignments/AssignmentGanttPage'
import { BulkAssignPage } from './pages/assignments/BulkAssignPage'
import { ContentItemDetailPage } from './pages/content-library/ContentItemDetailPage'
import { ContentItemEditorPage } from './pages/content-library/ContentItemEditorPage'
import { LearnerGroupCategoriesPage } from './pages/master-data/LearnerGroupCategoriesPage'
import { AdminUsersPage } from './pages/users/AdminUsersPage'

function LegacyStudentGroupsRedirect() {
  const location = useLocation()
  const nextPath = location.pathname.replace(/^\/student-groups/, '/learner-groups')
  return <Navigate to={`${nextPath}${location.search}${location.hash}`} replace />
}

export function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<DashboardPage />} />

        {/* Courses */}
        <Route path="courses" element={<CourseListPage />} />
        <Route path="courses/new" element={<CourseEditorPage />} />
        <Route path="courses/:id" element={<CourseDetailPage />} />
        <Route path="courses/:id/edit" element={<CourseEditorPage />} />
        <Route path="courses/:courseId/version/new" element={<VersionFormPage />} />
        <Route path="courses/:courseId/version/:id/edit" element={<VersionFormPage />} />

        {/* Content Library */}
        <Route path="content-library" element={<EntityListPage config={adminListConfigs.contentLibrary} />} />
        <Route path="content-library/new" element={<ContentItemEditorPage />} />
        <Route path="content-library/:id" element={<ContentItemDetailPage />} />
        <Route path="content-library/:id/edit" element={<ContentItemEditorPage />} />

        {/* Assignments */}
        <Route path="assignments" element={<EntityListPage config={adminListConfigs.assignments} />} />
        <Route path="assignments/gantt" element={<AssignmentGanttPage />} />
        <Route path="assignments/bulk" element={<BulkAssignPage />} />
        <Route path="assignments/:id" element={<AssignmentDetailPage />} />
        <Route path="assignments/:id/report" element={<AssignmentReportPage />} />

        {/* Learner Groups */}
        <Route path="learner-groups" element={<EntityListPage config={adminListConfigs.learnerGroups} />} />
        <Route path="learner-groups/new" element={<LearnerGroupEditorPage />} />
        <Route path="learner-groups/:id" element={<LearnerGroupDetailPage />} />
        <Route path="learner-groups/:id/edit" element={<LearnerGroupEditorPage />} />
        <Route path="student-groups/*" element={<LegacyStudentGroupsRedirect />} />

        {/* Learners */}
        <Route path="learners" element={<EntityListPage config={adminListConfigs.learners} />} />
        <Route path="learners/:id/profile" element={<LearnerProfilePage />} />

        {/* Operations */}
        <Route path="learning-logs" element={<EntityListPage config={adminListConfigs.learningLogs} />} />
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
          path="master-data/student-group-categories"
          element={<Navigate to="/master-data/learner-group-categories" replace />}
        />

        <Route
          path="system-config"
          element={
            <RequireRole superAdminOnly>
              <SystemConfigPage />
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
