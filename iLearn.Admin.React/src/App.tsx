import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { AccessDeniedPage } from './pages/AccessDeniedPage'
import { DashboardPage } from './pages/DashboardPage'
import { EntityListPage } from './pages/EntityListPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { adminListConfigs } from './pages/moduleConfigs'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="courses" element={<EntityListPage config={adminListConfigs.courses} />} />
        <Route path="content-library" element={<EntityListPage config={adminListConfigs.contentLibrary} />} />
        <Route path="assignments" element={<EntityListPage config={adminListConfigs.assignments} />} />
        <Route path="learner-groups" element={<EntityListPage config={adminListConfigs.learnerGroups} />} />
        <Route path="learners" element={<EntityListPage config={adminListConfigs.learners} />} />
        <Route path="master-data" element={<EntityListPage config={adminListConfigs.masterData} />} />
        <Route path="access-denied" element={<AccessDeniedPage />} />
        <Route path="not-found" element={<NotFoundPage />} />
        <Route path="*" element={<Navigate to="/not-found" replace />} />
      </Route>
    </Routes>
  )
}

export default App