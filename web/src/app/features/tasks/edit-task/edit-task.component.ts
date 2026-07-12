import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import type { ApiError, FieldErrors } from '../../../shared/utils/api-error-mapper';
import { mapApiErrorToFieldErrors } from '../../../shared/utils/api-error-mapper';
import { notBlankValidator } from '../../../shared/validators/not-blank.validator';
import { TaskService } from '../data-access/task.service';
import { DESCRIPTION_MAX_LENGTH, TITLE_MAX_LENGTH } from '../models/task.constants';
import { TASK_STATUSES } from '../models/task-status.enum';
import type { TaskResponse } from '../models/task.model';

@Component({
  selector: 'app-edit-task',
  standalone: true,
  imports: [ReactiveFormsModule, FormFieldComponent, RouterLink],
  templateUrl: './edit-task.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditTaskComponent {
  readonly titleMaxLength = TITLE_MAX_LENGTH;
  readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  readonly taskStatuses = TASK_STATUSES;

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly fieldErrors = signal<FieldErrors>({});
  readonly notFound = signal(false);

  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly taskId = signal<string | null>(null);

  readonly form = this.fb.group({
    title: ['', [Validators.required, notBlankValidator(), Validators.maxLength(TITLE_MAX_LENGTH)]],
    description: ['', [Validators.maxLength(DESCRIPTION_MAX_LENGTH)]],
    dueDate: [''],
    status: ['Pending', [Validators.required]],
  });

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');

      if (!id) {
        this.notFound.set(true);
        return;
      }

      this.taskId.set(id);
      this.loadTask(id);
    });
  }

  private loadTask(id: string): void {
    this.isLoading.set(true);
    this.notFound.set(false);

    this.taskService.getTaskById(id).subscribe({
      next: (task) => {
        this.patchForm(task);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 404) {
          this.notFound.set(true);
        }

        this.isLoading.set(false);
      },
    });
  }

  private patchForm(task: TaskResponse): void {
    this.form.patchValue({
      title: task.title,
      description: task.description ?? '',
      dueDate: this.toDateTimeLocal(task.dueDate),
      status: task.status,
    });
  }

  private toDateTimeLocal(iso: string | null): string {
    if (!iso) {
      return '';
    }

    const date = new Date(iso);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (n: number): string => n.toString().padStart(2, '0');
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  onSubmit(): void {
    this.form.markAllAsTouched();

    const id = this.taskId();

    if (this.form.invalid || !id) {
      return;
    }

    this.isSubmitting.set(true);
    this.fieldErrors.set({});

    const payload = this.buildPayload();

    this.taskService.updateTask(id, payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        void this.router.navigate(['/tasks', id]);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 400 && error.error) {
          this.fieldErrors.set(mapApiErrorToFieldErrors(error.error as ApiError));
        } else if (error.status === 404) {
          this.notFound.set(true);
        }

        this.isSubmitting.set(false);
      },
    });
  }

  private buildPayload(): {
    title?: string;
    description?: string;
    status?: string;
    dueDate?: string;
  } {
    const { title, description, dueDate, status } = this.form.getRawValue();
    const trimmedTitle = (title ?? '').trim();

    return {
      title: trimmedTitle,
      description: description?.trim() ? description.trim() : undefined,
      status: status ?? undefined,
      dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
    };
  }
}
