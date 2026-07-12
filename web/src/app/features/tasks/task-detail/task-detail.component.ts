import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { TaskService } from '../data-access/task.service';
import type { TaskResponse } from '../models/task.model';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './task-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailComponent {
  readonly task = signal<TaskResponse | null>(null);
  readonly notFound = signal(false);
  readonly isLoading = signal(true);

  private readonly taskService = inject(TaskService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');

      if (!id) {
        this.notFound.set(true);
        this.isLoading.set(false);
        return;
      }

      this.loadTask(id);
    });
  }

  private loadTask(id: string): void {
    this.isLoading.set(true);
    this.notFound.set(false);

    this.taskService.getTaskById(id).subscribe({
      next: (task) => {
        this.task.set(task);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 404) {
          this.notFound.set(true);
        }

        this.task.set(null);
        this.isLoading.set(false);
      },
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'In Progress':
        return 'bg-blue-100 text-blue-800';
      case 'Completed':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(iso: string | null): string {
    if (!iso) {
      return 'Not set';
    }

    const date = new Date(iso);

    if (Number.isNaN(date.getTime())) {
      return 'Not set';
    }

    return date.toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  onDelete(): void {
    const currentTask = this.task();

    if (!currentTask) {
      return;
    }

    if (!window.confirm('Are you sure you want to delete this task?')) {
      return;
    }

    this.taskService.deleteTask(currentTask.id).subscribe({
      next: () => {
        void this.router.navigate(['/tasks']);
      },
    });
  }
}
