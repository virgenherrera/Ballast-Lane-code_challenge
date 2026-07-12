import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/health/health.component').then(
        (m) => m.HealthComponent,
      ),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(
        (m) => m.LoginComponent,
      ),
  },
  {
    path: 'tasks/create',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tasks/create-task/create-task.component').then(
        (m) => m.CreateTaskComponent,
      ),
  },
  {
    path: 'tasks',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tasks/task-list/task-list.component').then(
        (m) => m.TaskListComponent,
      ),
  },
  {
    path: 'tasks/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tasks/task-detail/task-detail.component').then(
        (m) => m.TaskDetailComponent,
      ),
  },
];
