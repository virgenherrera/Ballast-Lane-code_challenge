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
