import { ADMIN_LABELS, COMMON_LABELS, contentTypeLabel, type LabelPair } from '../lib/labels'

export type AdminGridColumn = {
  dataField: string
  caption: LabelPair
  dataType?: 'string' | 'number' | 'boolean' | 'date' | 'datetime'
  width?: number
  minWidth?: number
  alignment?: 'left' | 'center' | 'right'
  visible?: boolean
  cellRender?: (cellInfo: { value: any; data: any; index: number }) => any
}

export type AdminListConfig = {
  title: LabelPair
  eyebrow: LabelPair
  description: LabelPair
  controller: string
  key: string
  gridTitle: LabelPair
  gridNote: LabelPair
  columns: AdminGridColumn[]
  /** Override the default `admin/{controller}` API base path */
  basePath?: string
  /** Column fields used for text search */
  searchExpr?: string[]
  hasDescription?: boolean
}

export const adminListConfigs = {
  courses: {
    title: ADMIN_LABELS.coursesTitle, eyebrow: ADMIN_LABELS.coursesEyebrow, description: ADMIN_LABELS.coursesDescription,
    controller: 'CoursesCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.coursesDirectory, gridNote: ADMIN_LABELS.coursesNote,
    columns: [
      { dataField: 'code', caption: ADMIN_LABELS.code, width: 120 }, { dataField: 'title', caption: ADMIN_LABELS.courseTitle, minWidth: 260 },
      { dataField: 'statusName', caption: ADMIN_LABELS.status, width: 120, alignment: 'center' }, { dataField: 'courseTypeName', caption: ADMIN_LABELS.type, width: 140, alignment: 'center' },
      { dataField: 'categoryName', caption: ADMIN_LABELS.category, minWidth: 180 }, { dataField: 'canAssign', caption: ADMIN_LABELS.assignPermission, dataType: 'boolean', width: 110, alignment: 'center' },
    ],
  },
  contentLibrary: {
    title: ADMIN_LABELS.contentLibraryTitle, eyebrow: ADMIN_LABELS.contentManagement, description: ADMIN_LABELS.contentLibraryDescription,
    controller: 'ContentItemsCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.scormPackages, gridNote: ADMIN_LABELS.scormPackagesNote,
    columns: [
      { dataField: 'name', caption: ADMIN_LABELS.contentName, minWidth: 260 }, { dataField: 'typeId', caption: ADMIN_LABELS.contentType, dataType: 'number', width: 130, alignment: 'center', cellRender: ({ value }: any) => value === 1 || value === 2 ? contentTypeLabel(value) : '—' },
      { dataField: 'schemaVersion', caption: ADMIN_LABELS.scorm, width: 120, alignment: 'center' }, { dataField: 'isActive', caption: ADMIN_LABELS.publishStatus, dataType: 'boolean', width: 130, alignment: 'center' },
      { dataField: 'launchHref', caption: ADMIN_LABELS.launchResource, minWidth: 220 }, { dataField: 'updatedAt', caption: ADMIN_LABELS.updated, dataType: 'datetime', width: 170 },
    ],
  },
  assignments: {
    title: ADMIN_LABELS.assignmentsTitle, eyebrow: ADMIN_LABELS.learningOperations, description: ADMIN_LABELS.assignmentsDescription,
    controller: 'AssignmentsCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.assignmentBatches, gridNote: ADMIN_LABELS.assignmentBatchesNote,
    searchExpr: ['assignmentNo', 'description'],
    columns: [
      { dataField: 'assignmentNo', caption: ADMIN_LABELS.assignmentNumber, width: 150 }, { dataField: 'description', caption: ADMIN_LABELS.description, minWidth: 220 }, { dataField: 'courseNames', caption: ADMIN_LABELS.courses, minWidth: 220 },
      // status + learnerCount are computed inside vw_AssignmentList (AssignmentListRow)
      { dataField: 'status', caption: ADMIN_LABELS.status, width: 120, alignment: 'center' }, { dataField: 'learnerCount', caption: ADMIN_LABELS.learners, dataType: 'number', width: 100, alignment: 'center' },
      { dataField: 'divisionId', caption: ADMIN_LABELS.division, dataType: 'number', width: 140 }, { dataField: 'startDate', caption: ADMIN_LABELS.startDate, dataType: 'date', width: 130 }, { dataField: 'dueDate', caption: ADMIN_LABELS.dueDate, dataType: 'date', width: 130 },
    ],
  },
  learners: {
    title: ADMIN_LABELS.learnersTitle, eyebrow: ADMIN_LABELS.peopleDirectory, description: ADMIN_LABELS.learnersDescription,
    controller: 'Learners',
    basePath: 'Learners',
    // EmployeeHub provider returns id=0 for every row — key on eId (unique employee code)
    // or AppTable's page>1 dedupe drops all new rows and infinite scroll never advances.
    key: 'eId',
    gridTitle: ADMIN_LABELS.learnerRegistry, gridNote: ADMIN_LABELS.learnerRegistryNote,
    // EmployeeHub provider filters in-memory via DataSourceLoader, so nid + Thai names are
    // searchable. (Legacy provider proxy 500s on nid — only relevant to local dev on Legacy.)
    searchExpr: ['thaiFirstName', 'thaiLastName', 'englishFirstName', 'englishLastName', 'eId', 'nid'],
    columns: [
      { dataField: 'eId', caption: ADMIN_LABELS.employeeId, width: 130 }, { dataField: 'nid', caption: ADMIN_LABELS.nid, width: 120 },
      { dataField: 'thaiFirstName', caption: ADMIN_LABELS.thaiFirstName, minWidth: 150 }, { dataField: 'thaiLastName', caption: ADMIN_LABELS.thaiLastName, minWidth: 150 },
      { dataField: 'englishFirstName', caption: ADMIN_LABELS.firstName, minWidth: 160 }, { dataField: 'englishLastName', caption: ADMIN_LABELS.lastName, minWidth: 160 },
      { dataField: 'division', caption: ADMIN_LABELS.division, width: 160 }, { dataField: 'department', caption: ADMIN_LABELS.department, width: 160 }, { dataField: 'section', caption: ADMIN_LABELS.section, width: 160 }, { dataField: 'position', caption: ADMIN_LABELS.position, minWidth: 180 },
    ],
  },
  users: {
    title: ADMIN_LABELS.adminUsersTitle, eyebrow: ADMIN_LABELS.accessIdentity, description: ADMIN_LABELS.adminUsersDescription,
    controller: 'UsersCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.administratorDirectory, gridNote: ADMIN_LABELS.administratorDirectoryNote,
    columns: [
      { dataField: 'nid', caption: ADMIN_LABELS.nid, width: 160 }, { dataField: 'fullName', caption: ADMIN_LABELS.displayName, minWidth: 220 },
      { dataField: 'lastLogin', caption: ADMIN_LABELS.lastLogin, dataType: 'datetime', width: 190 }, { dataField: 'isActive', caption: COMMON_LABELS.active, dataType: 'boolean', width: 100, alignment: 'center' }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 170 },
    ],
  },
  learningLogs: {
    title: ADMIN_LABELS.learningLogsTitle, eyebrow: ADMIN_LABELS.operations, description: ADMIN_LABELS.learningLogsDescription,
    controller: 'LearningLogsCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.scormLaunchAudit, gridNote: ADMIN_LABELS.scormLaunchAuditNote,
    searchExpr: ['status'],
    columns: [
      { dataField: 'id', caption: ADMIN_LABELS.logId, dataType: 'number', width: 100, alignment: 'center' }, { dataField: 'enrollmentId', caption: ADMIN_LABELS.enrollment, dataType: 'number', width: 120, alignment: 'center' }, { dataField: 'contentItemId', caption: ADMIN_LABELS.content, dataType: 'number', width: 110, alignment: 'center' },
      { dataField: 'status', caption: ADMIN_LABELS.status, width: 140, alignment: 'center' }, { dataField: 'score', caption: ADMIN_LABELS.score, dataType: 'number', width: 90, alignment: 'right' }, { dataField: 'updatedAt', caption: ADMIN_LABELS.updated, dataType: 'datetime', width: 180 },
    ],
  },
  enrollments: {
    title: ADMIN_LABELS.enrollmentsTitle, eyebrow: ADMIN_LABELS.operations, description: ADMIN_LABELS.enrollmentsDescription,
    controller: 'EnrollmentsCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.enrollmentLedger, gridNote: ADMIN_LABELS.enrollmentLedgerNote,
    // `status` is not server-filterable on EnrollmentsCRUD (returns 500) — search learnerCode only.
    searchExpr: ['learnerCode'],
    columns: [
      { dataField: 'id', caption: ADMIN_LABELS.id, dataType: 'number', width: 90, alignment: 'center' }, { dataField: 'learnerCode', caption: ADMIN_LABELS.learners, width: 140 }, { dataField: 'courseCode', caption: ADMIN_LABELS.courseCode, minWidth: 180 }, { dataField: 'courseTitle', caption: ADMIN_LABELS.courseTitle, minWidth: 260 },
      { dataField: 'isCompleted', caption: ADMIN_LABELS.status, width: 130, alignment: 'center' }, { dataField: 'progress', caption: ADMIN_LABELS.progressPercent, dataType: 'number', width: 110, alignment: 'right' }, { dataField: 'dueDate', caption: ADMIN_LABELS.dueDate, dataType: 'date', width: 140 }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 180 },
    ],
  },
  masterDataDivisions: {
    title: ADMIN_LABELS.divisionsTitle, eyebrow: ADMIN_LABELS.masterData, description: ADMIN_LABELS.divisionsDescription,
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.organizationalDivisions, gridNote: ADMIN_LABELS.organizationalDivisionsNote,
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: ADMIN_LABELS.divisionName, minWidth: 260 }, { dataField: 'categoryCount', caption: ADMIN_LABELS.categories, dataType: 'number', width: 110, alignment: 'center' }, { dataField: 'roleCount', caption: ADMIN_LABELS.roles, dataType: 'number', width: 90, alignment: 'center' }, { dataField: 'isActive', caption: COMMON_LABELS.active, dataType: 'boolean', width: 100, alignment: 'center' }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 170 },
    ],
  },
  masterDataCategories: {
    title: ADMIN_LABELS.categoriesTitle, eyebrow: ADMIN_LABELS.masterData, description: ADMIN_LABELS.categoriesDescription,
    controller: 'CategoriesCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.courseCategories, gridNote: ADMIN_LABELS.courseCategoriesNote,
    hasDescription: true,
    searchExpr: ['name', 'description'],
    columns: [
      { dataField: 'sortOrder', caption: ADMIN_LABELS.sortOrder, dataType: 'number', width: 90, alignment: 'center' }, { dataField: 'name', caption: ADMIN_LABELS.categoryName, minWidth: 260 }, { dataField: 'description', caption: ADMIN_LABELS.description, minWidth: 220 }, { dataField: 'divisionName', caption: ADMIN_LABELS.division, width: 150 }, { dataField: 'courseCount', caption: ADMIN_LABELS.courses, dataType: 'number', width: 100, alignment: 'center' }, { dataField: 'isActive', caption: COMMON_LABELS.active, dataType: 'boolean', width: 100, alignment: 'center' }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 170 },
    ],
  },
  masterDataCourseTypes: {
    title: ADMIN_LABELS.courseTypesTitle, eyebrow: ADMIN_LABELS.masterData, description: ADMIN_LABELS.courseTypesDescription,
    controller: 'CourseTypesCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.courseTypesTitle, gridNote: ADMIN_LABELS.courseTypesNote,
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: ADMIN_LABELS.typeName, minWidth: 260 }, { dataField: 'description', caption: ADMIN_LABELS.description, minWidth: 200 }, { dataField: 'courseCount', caption: ADMIN_LABELS.courses, dataType: 'number', width: 100, alignment: 'center' }, { dataField: 'isActive', caption: COMMON_LABELS.active, dataType: 'boolean', width: 100, alignment: 'center' }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 170 },
    ],
  },
  masterDataRoles: {
    title: ADMIN_LABELS.rolesTitle, eyebrow: ADMIN_LABELS.masterData, description: ADMIN_LABELS.rolesDescription,
    controller: 'RolesCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.administrativeRoles, gridNote: ADMIN_LABELS.administrativeRolesNote,
    // Role entity has no Description property — search by name only.
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: ADMIN_LABELS.roleName, minWidth: 220 }, { dataField: 'roleType', caption: ADMIN_LABELS.type, dataType: 'number', width: 80, alignment: 'center' }, { dataField: 'division', caption: ADMIN_LABELS.division, width: 140 }, { dataField: 'isActive', caption: COMMON_LABELS.active, dataType: 'boolean', width: 100, alignment: 'center' }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 170 },
    ],
  },
  // Backward-compat alias for old EntityListPage route /master-data
  masterData: {
    title: ADMIN_LABELS.divisionsTitle, eyebrow: ADMIN_LABELS.masterData, description: ADMIN_LABELS.defaultMasterDataDescription,
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: ADMIN_LABELS.organizationalDivisions, gridNote: ADMIN_LABELS.superAdminOnly,
    searchExpr: ['name'],
    columns: [
      { dataField: 'name', caption: ADMIN_LABELS.divisionName, minWidth: 260 }, { dataField: 'categoryCount', caption: ADMIN_LABELS.categories, dataType: 'number', width: 110, alignment: 'center' }, { dataField: 'roleCount', caption: ADMIN_LABELS.roles, dataType: 'number', width: 90, alignment: 'center' }, { dataField: 'isActive', caption: COMMON_LABELS.active, dataType: 'boolean', width: 100, alignment: 'center' }, { dataField: 'createdAt', caption: ADMIN_LABELS.created, dataType: 'datetime', width: 170 },
    ],
  },
} satisfies Record<string, AdminListConfig>
