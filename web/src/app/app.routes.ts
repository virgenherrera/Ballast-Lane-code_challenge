import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/health/health.component').then(
        (m) => m.HealthComponent,
      ),
  },
  {
    path: 'tasks/create',
    loadComponent: () =>
      import('./features/tasks/create-task/create-task.component').then(
        (m) => m.CreateTaskComponent,
      ),
  },
  // TEMPORARY route — superseded by US-005 (real task list view).
  {
    path: 'tasks',
    loadComponent: () =>
      import('./features/tasks/task-list-stub/task-list-stub.component').then(
        (m) => m.TaskListStubComponent,
      ),
  },
];
