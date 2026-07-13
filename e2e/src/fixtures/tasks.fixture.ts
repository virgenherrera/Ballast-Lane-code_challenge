import { test as base } from './auth.fixture.js';
import { CreateTaskPage } from '../pages/create-task.page.js';
import { TaskDetailPage } from '../pages/task-detail.page.js';
import { TaskListPage } from '../pages/task-list.page.js';

interface TaskFixtures {
  taskListPage: TaskListPage;
  createTaskPage: CreateTaskPage;
  taskDetailPage: TaskDetailPage;
}

export const test = base.extend<TaskFixtures>({
  taskListPage: async ({ authenticatedPage }, use) => {
    await use(new TaskListPage(authenticatedPage));
  },

  createTaskPage: async ({ authenticatedPage }, use) => {
    await use(new CreateTaskPage(authenticatedPage));
  },

  taskDetailPage: async ({ authenticatedPage }, use) => {
    await use(new TaskDetailPage(authenticatedPage));
  },
});

export { expect } from '@playwright/test';
