export const SELECTORS = {
  // Task list
  taskList: '[data-testid="task-list"]',
  taskListItem: '[data-testid="task-list-item"]',
  statusFilter: '[data-testid="status-filter"]',
  emptyState: '[data-testid="empty-state"]',
  pagePrev: '[data-testid="page-prev"]',
  pageNext: '[data-testid="page-next"]',
  statusSelectById: (id: string): string => `[data-testid="status-select-${id}"]`,
  deleteBtnById: (id: string): string => `[data-testid="delete-btn-${id}"]`,

  // Create task
  titleInput: '#title',
  descriptionInput: '#description',
  dueDateInput: '#dueDate',

  // Task detail
  taskDetail: '[data-testid="task-detail"]',
} as const;
