import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { TaskService } from '../data-access/task.service';
import type { TaskListItem, Paging } from '../models/task.model';
import { TaskStatusFilterComponent } from '../task-status-filter/task-status-filter.component';

const DEFAULT_PAGE = 1;

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [RouterLink, TaskStatusFilterComponent],
  templateUrl: './task-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent {
  readonly tasks = signal<TaskListItem[]>([]);
  readonly paging = signal<Paging | null>(null);
  readonly status = signal<string | null>(null);
  readonly page = signal<number>(DEFAULT_PAGE);
  readonly isLoading = signal(false);

  private readonly taskService = inject(TaskService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const status = params.get('status');
      const pageParam = params.get('page');
      const page = pageParam ? Number(pageParam) : DEFAULT_PAGE;

      this.status.set(status);
      this.page.set(Number.isFinite(page) && page > 0 ? page : DEFAULT_PAGE);

      this.fetchTasks();
    });
  }

  onStatusChange(status: string | null): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { status: status ?? null, page: null },
      queryParamsHandling: 'merge',
    });
  }

  onPrevPage(): void {
    this.navigateToPage(this.page() - 1);
  }

  onNextPage(): void {
    this.navigateToPage(this.page() + 1);
  }

  private navigateToPage(page: number): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page },
      queryParamsHandling: 'merge',
    });
  }

  private fetchTasks(): void {
    this.isLoading.set(true);

    const status = this.status();

    this.taskService
      .getTasks({
        ...(status ? { status } : {}),
        page: this.page(),
      })
      .subscribe({
        next: (response) => {
          this.tasks.set(response.items);
          this.paging.set(response.paging);
          this.isLoading.set(false);
        },
        error: () => {
          this.tasks.set([]);
          this.paging.set(null);
          this.isLoading.set(false);
        },
      });
  }
}
