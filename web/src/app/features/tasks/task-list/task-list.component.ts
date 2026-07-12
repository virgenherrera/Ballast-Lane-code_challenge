import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { TaskService } from '../data-access/task.service';
import { TASK_STATUSES } from '../models/task-status.enum';
import type { TaskListItem, Paging } from '../models/task.model';

const DEFAULT_PAGE = 1;

interface StatusGroup {
  label: string;
  items: TaskListItem[];
}

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './task-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent {
  readonly tasks = signal<TaskListItem[]>([]);
  readonly paging = signal<Paging | null>(null);
  readonly status = signal<string | null>(null);
  readonly page = signal<number>(DEFAULT_PAGE);
  readonly isLoading = signal(false);
  readonly taskStatuses = TASK_STATUSES;

  /** Tab UI concept — mirrors `status()` 1:1; the active tab IS the active status filter. */
  readonly activeTab = computed(() => this.status());

  /**
   * Groups the CURRENT page's tasks by status for the "All Tasks" view.
   * Safe because it only groups already-fetched data (max `perPage` items),
   * never triggers an extra request.
   */
  readonly groupedEntries = computed<StatusGroup[]>(() => {
    const items = this.tasks();

    return this.taskStatuses.map((label) => ({
      label,
      items: items.filter((task) => task.status === label),
    }));
  });

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

  /** Handler for the sr-only native <select> — E2E contract (`status-filter`). */
  onTabSelectChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;

    this.onStatusChange(value === 'All' ? null : value);
  }

  onStatusUpdate(taskId: string, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;

    this.taskService.updateTask(taskId, { status: value }).subscribe({
      next: () => this.fetchTasks(),
    });
  }

  /** Cycles a task to the next status in TASK_STATUSES order (wrapping), via the same update path. */
  cycleStatus(task: TaskListItem): void {
    const currentIndex = this.taskStatuses.indexOf(task.status as (typeof this.taskStatuses)[number]);
    const nextIndex = (currentIndex + 1) % this.taskStatuses.length;
    const nextStatus = this.taskStatuses[nextIndex];

    this.taskService.updateTask(task.id, { status: nextStatus }).subscribe({
      next: () => this.fetchTasks(),
    });
  }

  formatDueDate(dueDate: string | null): string {
    if (!dueDate) {
      return 'No due date';
    }

    const date = new Date(dueDate);

    if (Number.isNaN(date.getTime())) {
      return 'No due date';
    }

    const today = new Date();
    const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const startOfDue = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const diffDays = Math.round((startOfDue.getTime() - startOfToday.getTime()) / 86400000);

    if (diffDays === 0) {
      return 'Today';
    }

    if (diffDays === 1) {
      return 'Tomorrow';
    }

    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  }

  onDelete(task: TaskListItem): void {
    if (!window.confirm(`Delete task "${task.title}"?`)) {
      return;
    }

    this.taskService.deleteTask(task.id).subscribe({
      next: () => this.fetchTasks(),
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
