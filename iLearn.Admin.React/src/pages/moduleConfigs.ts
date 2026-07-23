import { contentTypeLabel } from '../lib/labels'

export type AdminGridColumn = {
  dataField: string
  caption: string
  dataType?: 'string' | 'number' | 'boolean' | 'date' | 'datetime'
  width?: number
  minWidth?: number
  alignment?: 'left' | 'center' | 'right'
  visible?: boolean
  cellRender?: (cellInfo: { value: any; data: any; index: number }) => any
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
  /** Override the default `admin/{controller}` API base path */
  basePath?: string
  /** Column fields used for text search */
  searchExpr?: string[]
  hasDescription?: boolean
}

export const adminListConfigs = {
  courses: {
    title: 'Courses',
    eyebrow: 'Course Management',
    description: 'Manage training courses, catalog taxonomy, and assignment status.',
    controller: 'CoursesCRUD',
    key: 'id',
    gridTitle: 'Courses Directory',
    gridNote: 'Catalog, status, and readiness of training courses.',
    columns: [
      { dataField: 'code', caption: 'Code', width: 120 },
      { dataField: 'title', caption: 'Course Title', minWidth: 260 },
      { dataField: 'statusName', caption: 'Status', width: 120, alignment: 'center' },
      { dataField: 'courseTypeName', caption: 'Type', width: 140, alignment: 'center' },
      { dataField: 'categoryName', caption: 'Category', minWidth: 180 },
      { dataField: 'canAssign', caption: 'สิทธิ์มอบหมาย', dataType: 'boolean', width: 110, alignment: 'center' },
    ],
  },
  contentLibrary: {
    title: 'Content Library',
    eyebrow: 'Content Management',
    description: 'SCORM package library, publishing status, and launch configurations.',
    controller: 'ContentItemsCRUD',
    key: 'id',
    gridTitle: 'SCORM Packages',
    gridNote: 'Uploaded interactive SCORM packages and launch parameters.',
    columns: [
      { dataField: 'name', caption: 'Content Name', minWidth: 260 },
      { dataField: 'typeId', caption: 'Content Type', dataType: 'number', width: 130, alignment: 'center', cellRender: ({ value }: any) => value === 1 || value === 2 ? contentTypeLabel(value) : '—' },
      { dataField: 'schemaVersion', caption: 'SCORM', width: 120, alignment: 'center' },
      { dataField: 'isActive', caption: 'สถานะการเผยแพร่', dataType: 'boolean', width: 130, alignment: 'center' },
      { dataField: 'launchHref', caption: 'Launch Resource', minWidth: 220 },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
  assignments: {
    title: 'Assignments',
    eyebrow: 'Learning Operations',
    description: 'Deploy training courses to target learners and track dispatch status.',
    controller: 'AssignmentsCRUD',
    key: 'id',
    gridTitle: 'Assignment Batches',
    gridNote: 'Training deployment batches, schedule windows, and scoping rules.',
    searchExpr: ['assignmentNo', 'description'],
    columns: [
      { dataField: 'assignmentNo', caption: 'Assignment No.', width: 150 },
      { dataField: 'description', caption: 'Description', minWidth: 220 },
      { dataField: 'courseNames', caption: 'Courses', minWidth: 220 },
      // status + learnerCount are computed inside vw_AssignmentList (AssignmentListRow)
      { dataField: 'status', caption: 'Status', width: 120, alignment: 'center' },
      { dataField: 'learnerCount', caption: 'Learners', dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'divisionId', caption: 'Division', dataType: 'number', width: 140 },
      { dataField: 'startDate', caption: 'Start Date', dataType: 'date', width: 130 },
      { dataField: 'dueDate', caption: 'Due Date', dataType: 'date', width: 130 },
    ],
  },
  learners: {
    title: 'Learners',
    eyebrow: 'People Directory',
    description: 'Search and view employee profiles, divisions, and learning history.',
    controller: 'Learners',
    basePath: 'Learners',
    key: 'id',
    gridTitle: 'Learner Registry',
    gridNote: 'All active learners synchronized from the corporate registry.',
    // NID is not filterable on the external employee grid endpoint (it 500s),
    // so it is intentionally excluded from search. Search EId + names only.
    searchExpr: ['englishFirstName', 'englishLastName', 'eId'],
    columns: [
      { dataField: 'eId', caption: 'Employee ID', width: 130 },
      { dataField: 'nid', caption: 'NID', width: 120 },
      { dataField: 'englishFirstName', caption: 'First Name', minWidth: 160 },
      { dataField: 'englishLastName', caption: 'Last Name', minWidth: 160 },
      { dataField: 'division', caption: 'Division', width: 160 },
      { dataField: 'department', caption: 'Department', width: 160 },
      { dataField: 'section', caption: 'Section', width: 160 },
      { dataField: 'position', caption: 'Position', minWidth: 180 },
    ],
  },
  users: {
    title: 'Admin Users',
    eyebrow: 'Access & Identity',
    description: 'Manage administrator access control and role assignments.',
    controller: 'UsersCRUD',
    key: 'id',
    gridTitle: 'Administrator Directory',
    gridNote: 'Authorized administrative accounts and access privileges.',
    columns: [
      { dataField: 'nid', caption: 'NID', width: 160 },
      { dataField: 'fullName', caption: 'Display Name', minWidth: 220 },
      { dataField: 'lastLogin', caption: 'Last Login', dataType: 'datetime', width: 190 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 170 },
    ],
  },
  learningLogs: {
    title: 'Learning Logs',
    eyebrow: 'Operations',
    description: 'Real-time audit log of learner course interactions and content launches.',
    controller: 'LearningLogsCRUD',
    key: 'id',
    gridTitle: 'SCORM Launch Audit',
    gridNote: 'Audit trail of learner launch attempts and SCORM runtime status.',
    searchExpr: ['status'],
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
    description: 'Overview of learner course enrollments, completion rates, and status.',
    controller: 'EnrollmentsCRUD',
    key: 'id',
    gridTitle: 'Enrollment Ledger',
    gridNote: 'Detailed enrollment records and overall progress status.',
    // `status` is not server-filterable on EnrollmentsCRUD (returns 500) — search learnerCode only.
    searchExpr: ['learnerCode'],
    columns: [
      { dataField: 'id', caption: 'ID', dataType: 'number', width: 90, alignment: 'center' },
      { dataField: 'learnerCode', caption: 'Learner', width: 140 },
      { dataField: 'courseCode', caption: 'Course Code', minWidth: 180 },
      { dataField: 'courseTitle', caption: 'Course Title', minWidth: 260 },
      { dataField: 'isCompleted', caption: 'Status', width: 130, alignment: 'center' },
      { dataField: 'progress', caption: 'Progress %', dataType: 'number', width: 110, alignment: 'right' },
      { dataField: 'dueDate', caption: 'Due Date', dataType: 'date', width: 140 },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 180 },
    ],
  },
  masterDataDivisions: {
    title: 'Divisions',
    eyebrow: 'Master Data',
    description: 'Configure business divisions used across the platform.',
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: 'Organizational Divisions',
    gridNote: 'Corporate divisions for learner scoping and reporting boundaries.',
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: 'Division Name', minWidth: 260 },
      { dataField: 'categoryCount', caption: 'Categories', dataType: 'number', width: 110, alignment: 'center' },
      { dataField: 'roleCount', caption: 'Roles', dataType: 'number', width: 90, alignment: 'center' },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 170 },
    ],
  },
  masterDataCategories: {
    title: 'Categories',
    eyebrow: 'Master Data',
    description: 'Configure training categories for organization.',
    controller: 'CategoriesCRUD',
    key: 'id',
    gridTitle: 'Course Categories',
    gridNote: 'Taxonomy categories used for grouping courses in the catalog.',
    hasDescription: true,
    searchExpr: ['name', 'description'],
    columns: [
      { dataField: 'sortOrder', caption: 'ลำดับ', dataType: 'number', width: 90, alignment: 'center' },
      { dataField: 'name', caption: 'Category Name', minWidth: 260 },
      { dataField: 'description', caption: 'Description', minWidth: 220 },
      { dataField: 'divisionName', caption: 'Division', width: 150 },
      { dataField: 'courseCount', caption: 'Courses', dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 170 },
    ],
  },
  masterDataCourseTypes: {
    title: 'Course Types',
    eyebrow: 'Master Data',
    description: 'Configure course types that drive visual badges and filtering.',
    controller: 'CourseTypesCRUD',
    key: 'id',
    gridTitle: 'Course Types',
    gridNote: 'Classification values such as Mandatory, Optional, or Compliance.',
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: 'Type Name', minWidth: 260 },
      { dataField: 'description', caption: 'Description', minWidth: 200 },
      { dataField: 'courseCount', caption: 'Courses', dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 170 },
    ],
  },
  masterDataRoles: {
    title: 'Roles',
    eyebrow: 'Master Data',
    description: 'View application roles used for permission gating.',
    controller: 'RolesCRUD',
    key: 'id',
    gridTitle: 'Administrative Roles',
    gridNote: 'System roles driving policy enforcement and access control.',
    // Role entity has no Description property — search by name only.
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: 'Role Name', minWidth: 220 },
      { dataField: 'roleType', caption: 'Type', dataType: 'number', width: 80, alignment: 'center' },
      { dataField: 'division', caption: 'Division', width: 140 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 170 },
    ],
  },
  // Backward-compat alias for old EntityListPage route /master-data
  masterData: {
    title: 'Divisions',
    eyebrow: 'Master Data',
    description: 'Default master-data view. Use the sidebar to switch between divisions, categories, course types, and roles.',
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: 'Organizational Divisions',
    gridNote: 'SuperAdmin only.',
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: 'Division Name', minWidth: 260 },
      { dataField: 'categoryCount', caption: 'Categories', dataType: 'number', width: 110, alignment: 'center' },
      { dataField: 'roleCount', caption: 'Roles', dataType: 'number', width: 90, alignment: 'center' },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'createdAt', caption: 'Created', dataType: 'datetime', width: 170 },
    ],
  },
} satisfies Record<string, AdminListConfig>
