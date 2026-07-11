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
  {
    path: 'tasks',
    loadComponent: () =>
      import('./features/tasks/task-list/task-list.component').then(
        (m) => m.TaskListComponent,
      ),
  },
  {
    path: 'tasks/:id',
    loadComponent: () =>
      import('./features/tasks/task-detail/task-detail.component').then(
        (m) => m.TaskDetailComponent,
      ),
  },
];
