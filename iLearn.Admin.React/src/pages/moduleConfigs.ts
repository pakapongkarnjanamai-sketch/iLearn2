export type AdminGridColumn = {
  dataField: string
  caption: string
  dataType?: 'string' | 'number' | 'boolean' | 'date' | 'datetime'
  width?: number
  minWidth?: number
  alignment?: 'left' | 'center' | 'right'
  visible?: boolean
}

export type AdminListConfig = {
  title: string
  eyebrow: string
  description: string
  controller: string
  key: string
  gridTitle: string
  gridNote: string
  columns: AdminGridColumn[]
}

export const adminListConfigs = {
  courses: {
    title: 'Courses',
    eyebrow: 'Course Management',
    description: 'Catalog, lifecycle status, course type, category, and assignment readiness.',
    controller: 'CoursesCRUD',
    key: 'id',
    gridTitle: 'Course Directory',
    gridNote: 'Server-side filtering, sorting, and paging through the existing Admin API.',
    columns: [
      { dataField: 'code', caption: 'Code', width: 120 },
      { dataField: 'title', caption: 'Course Title', minWidth: 260 },
      { dataField: 'status', caption: 'Status', width: 120, alignment: 'center' },
      { dataField: 'courseTypeId', caption: 'Type', dataType: 'number', width: 90, alignment: 'center' },
      { dataField: 'categoryId', caption: 'Category', dataType: 'number', width: 110, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  contentLibrary: {
    title: 'Content Library',
    eyebrow: 'Content Management',
    description: 'SCORM packages, launch metadata, publishing state, and Learn or Exam content type.',
    controller: 'ContentItemsCRUD',
    key: 'id',
    gridTitle: 'Content Items',
    gridNote: 'Readiness details stay owned by the API and lifecycle services.',
    columns: [
      { dataField: 'name', caption: 'Content Name', minWidth: 260 },
      { dataField: 'typeId', caption: 'Content Type', dataType: 'number', width: 130, alignment: 'center' },
      { dataField: 'schemaVersion', caption: 'SCORM', width: 120, alignment: 'center' },
      { dataField: 'isActive', caption: 'Published', dataType: 'boolean', width: 120, alignment: 'center' },
      { dataField: 'launchHref', caption: 'Launch Resource', minWidth: 220 },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  assignments: {
    title: 'Assignments',
    eyebrow: 'Learning Operations',
    description: 'Assignment batches, date windows, learner scope, and course assignment history.',
    controller: 'AssignmentsCRUD',
    key: 'id',
    gridTitle: 'Assignment Batches',
    gridNote: 'Assignment status is computed server-side; React only presents the result.',
    columns: [
      { dataField: 'assignmentNo', caption: 'Assignment No.', width: 150 },
      { dataField: 'description', caption: 'Description', minWidth: 240 },
      { dataField: 'courseId', caption: 'Course', dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'division', caption: 'Division', width: 150 },
      { dataField: 'startDate', caption: 'Start Date', dataType: 'date', width: 140 },
      { dataField: 'dueDate', caption: 'Due Date', dataType: 'date', width: 140 },
    ],
  },
  learnerGroups: {
    title: 'Learner Groups',
    eyebrow: 'Learner Segmentation',
    description: 'Managed learner groups used for assignments and membership workflows.',
    controller: 'LearnerGroupsCRUD',
    key: 'id',
    gridTitle: 'Learner Group Directory',
    gridNote: 'Membership actions use the Learner Group detail page.',
    columns: [
      { dataField: 'name', caption: 'Group Name', minWidth: 260 },
      { dataField: 'description', caption: 'Description', minWidth: 260 },
      { dataField: 'divisionId', caption: 'Division', dataType: 'number', width: 110, alignment: 'center' },
      { dataField: 'categoryId', caption: 'Category', dataType: 'number', width: 120, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  learners: {
    title: 'Learners',
    eyebrow: 'People Directory',
    description: 'Admin users and learner identities resolved through the existing Windows-auth API contracts.',
    controller: 'UsersCRUD',
    key: 'id',
    gridTitle: 'User Directory',
    gridNote: 'Role and division claims remain enforced by the API.',
    columns: [
      { dataField: 'nid', caption: 'NID', width: 160 },
      { dataField: 'lastLogin', caption: 'Last Login', dataType: 'datetime', width: 190 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  users: {
    title: 'Admin Users',
    eyebrow: 'Access & Identity',
    description: 'Windows-auth principals with admin or SuperAdmin role assignments. Roles are enforced by the API.',
    controller: 'UsersCRUD',
    key: 'id',
    gridTitle: 'Admin User Directory',
    gridNote: 'Same backing data as Learners, focused on role and access auditing.',
    columns: [
      { dataField: 'nid', caption: 'NID', width: 160 },
      { dataField: 'displayName', caption: 'Display Name', minWidth: 220 },
      { dataField: 'lastLogin', caption: 'Last Login', dataType: 'datetime', width: 190 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  learningLogs: {
    title: 'Learning Logs',
    eyebrow: 'Operations',
    description: 'Per-launch attempt records, runtime status, and audit trail for SCORM content items.',
    controller: 'LearningLogsCRUD',
    key: 'id',
    gridTitle: 'Learning Log Entries',
    gridNote: 'Read-only audit feed. Drill into an enrollment to inspect lifecycle context.',
    columns: [
      { dataField: 'id', caption: 'Log ID', dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'enrollmentId', caption: 'Enrollment', dataType: 'number', width: 120, alignment: 'center' },
      { dataField: 'contentItemId', caption: 'Content', dataType: 'number', width: 110, alignment: 'center' },
      { dataField: 'status', caption: 'Status', width: 140, alignment: 'center' },
      { dataField: 'score', caption: 'Score', dataType: 'number', width: 90, alignment: 'right' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 180 },
    ],
  },
  enrollments: {
    title: 'Enrollments',
    eyebrow: 'Operations',
    description: 'Cross-batch enrollment ledger. Used for ad-hoc reset and lifecycle inspection.',
    controller: 'EnrollmentsCRUD',
    key: 'id',
    gridTitle: 'Enrollment Records',
    gridNote: 'SuperAdmin only. Resetting enrollments is destructive and tracked through audit logs.',
    columns: [
      { dataField: 'id', caption: 'ID', dataType: 'number', width: 90, alignment: 'center' },
      { dataField: 'assignmentId', caption: 'Assignment', dataType: 'number', width: 120, alignment: 'center' },
      { dataField: 'learnerCode', caption: 'Learner', width: 140 },
      { dataField: 'courseId', caption: 'Course', dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'status', caption: 'Status', width: 140, alignment: 'center' },
      { dataField: 'progress', caption: 'Progress %', dataType: 'number', width: 110, alignment: 'right' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 180 },
    ],
  },
  masterDataDivisions: {
    title: 'Divisions',
    eyebrow: 'Master Data',
    description: 'Organizational divisions. Used for learner scoping and admin access control.',
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: 'Division Directory',
    gridNote: 'Inline edit available. SuperAdmin only.',
    columns: [
      { dataField: 'name', caption: 'Division Name', minWidth: 260 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  masterDataCategories: {
    title: 'Categories',
    eyebrow: 'Master Data',
    description: 'Course categories used for catalog grouping and reporting.',
    controller: 'CategoriesCRUD',
    key: 'id',
    gridTitle: 'Category Directory',
    gridNote: 'Inline edit available. SuperAdmin only.',
    columns: [
      { dataField: 'name', caption: 'Category Name', minWidth: 260 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  masterDataCourseTypes: {
    title: 'Course Types',
    eyebrow: 'Master Data',
    description: 'Course type lookup (e.g. Mandatory, Optional, Compliance). Drives badges and filters.',
    controller: 'CourseTypesCRUD',
    key: 'id',
    gridTitle: 'Course Type Directory',
    gridNote: 'Inline edit available. SuperAdmin only.',
    columns: [
      { dataField: 'name', caption: 'Type Name', minWidth: 260 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  masterDataRoles: {
    title: 'Roles',
    eyebrow: 'Master Data',
    description: 'Application roles. Driver of admin-level UI and policy enforcement.',
    controller: 'RolesCRUD',
    key: 'id',
    gridTitle: 'Role Directory',
    gridNote: 'Inline edit available. SuperAdmin only. Role membership is managed elsewhere.',
    columns: [
      { dataField: 'name', caption: 'Role Name', minWidth: 220 },
      { dataField: 'description', caption: 'Description', minWidth: 280 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  // Backward-compat alias for old EntityListPage route /master-data
  masterData: {
    title: 'Divisions',
    eyebrow: 'Master Data',
    description: 'Default master-data view. Use the sidebar to switch between divisions, categories, course types, and roles.',
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: 'Division Directory',
    gridNote: 'SuperAdmin only.',
    columns: [
      { dataField: 'name', caption: 'Division Name', minWidth: 260 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
} satisfies Record<string, AdminListConfig>
