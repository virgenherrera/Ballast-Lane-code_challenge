export type TaskStatusFilter = 'Pending' | 'In Progress' | 'Completed';

export const TASK_STATUSES: readonly TaskStatusFilter[] = [
  'Pending',
  'In Progress',
  'Completed',
] as const;
