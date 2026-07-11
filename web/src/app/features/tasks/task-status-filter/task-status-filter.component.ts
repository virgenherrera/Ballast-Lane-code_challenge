import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { TASK_STATUSES } from '../models/task-status.enum';

export const ALL_STATUS_OPTION = 'All' as const;

@Component({
  selector: 'app-task-status-filter',
  standalone: true,
  templateUrl: './task-status-filter.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskStatusFilterComponent {
  readonly currentStatus = input<string | null>(null);
  readonly statusChange = output<string | null>();

  readonly allOption = ALL_STATUS_OPTION;
  readonly statuses = TASK_STATUSES;

  onChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;

    this.statusChange.emit(value === ALL_STATUS_OPTION ? null : value);
  }
}
