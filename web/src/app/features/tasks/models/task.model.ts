export interface Task {
  id: string;
  title: string;
  description: string | null;
  status: string;
  dueDate: string | null;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  dueDate?: string | null;
}

export type TaskResponse = Task;

export interface TaskListItem {
  id: string;
  title: string;
  status: string;
  dueDate: string | null;
}

export interface Paging {
  page: number;
  perPage: number;
  total: number;
  prev: string | null;
  next: string | null;
}

export interface TaskListResponse {
  items: TaskListItem[];
  paging: Paging;
}
