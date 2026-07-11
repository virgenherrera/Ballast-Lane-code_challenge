import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

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
}
