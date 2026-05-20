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
    title: 'Student Groups',
    eyebrow: 'Learner Segmentation',
    description: 'Managed learner groups used for assignments and membership workflows.',
    controller: 'LearnerGroupsCRUD',
    key: 'id',
    gridTitle: 'Student Group Directory',
    gridNote: 'Membership actions will be migrated after selection trays and conflict handling are ready.',
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
  masterData: {
    title: 'Master Data',
    eyebrow: 'System Administration',
    description: 'Divisions, categories, roles, course types, and lookup values used across the LMS.',
    controller: 'DivisionsCRUD',
    key: 'id',
    gridTitle: 'Division Directory',
    gridNote: 'This route starts with divisions; other master-data pages can reuse the same list shell.',
    columns: [
      { dataField: 'name', caption: 'Division Name', minWidth: 260 },
      { dataField: 'isActive', caption: 'Active', dataType: 'boolean', width: 100, alignment: 'center' },
      { dataField: 'updatedAt', caption: 'Updated', dataType: 'datetime', width: 170 },
    ],
  },
} satisfies Record<string, AdminListConfig>