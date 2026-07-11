import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { TaskService } from '../data-access/task.service';
import type { Task } from '../models/task.model';

// TEMPORARY: minimal stub to unblock EP01-B1-06 E2E coverage.
// Superseded by US-005 (list/filter/paginate tasks) — no styling, no
// filtering, no sorting. Titles + status dropdown only.
@Component({
  selector: 'app-task-list-stub',
  standalone: true,
  templateUrl: './task-list-stub.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListStubComponent {
  readonly tasks = signal<Task[]>([]);

  private readonly taskService = inject(TaskService);

  constructor() {
    this.taskService.getTasks().subscribe((tasks) => this.tasks.set(tasks));
  }

  onStatusChange(taskId: string, event: Event): void {
    const status = (event.target as HTMLSelectElement).value;

    this.taskService.updateTask(taskId, { status }).subscribe((updated) => {
      this.tasks.update((tasks) =>
        tasks.map((task) => (task.id === taskId ? updated : task)),
      );
    });
  }

  onDelete(taskId: string): void {
    if (!confirm('Are you sure you want to delete this task?')) {
      return;
    }

    this.taskService.deleteTask(taskId).subscribe({
      next: () => {
        this.tasks.update((tasks) => tasks.filter((t) => t.id !== taskId));
      },
      error: () => {
        // On failure, task remains in list — no state change.
        // Error handling can be enhanced in future stories.
      },
    });
  }
}
