import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { TaskService } from '../data-access/task.service';
import type { Task } from '../models/task.model';

// TEMPORARY: minimal stub to unblock EP01-B1-06 E2E coverage.
// Superseded by US-005 (list/filter/paginate tasks) — no styling, no
// filtering, no sorting. Titles only.
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
}
