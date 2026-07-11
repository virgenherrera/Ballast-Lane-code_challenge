import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { HttpErrorResponse } from '@angular/common/http';

import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import type { ApiError, FieldErrors } from '../../../shared/utils/api-error-mapper';
import { mapApiErrorToFieldErrors } from '../../../shared/utils/api-error-mapper';
import { futureDateValidator } from '../../../shared/validators/future-date.validator';
import { notBlankValidator } from '../../../shared/validators/not-blank.validator';
import { TaskService } from '../data-access/task.service';
import { DESCRIPTION_MAX_LENGTH, TITLE_MAX_LENGTH } from '../models/task.constants';
import type { CreateTaskRequest } from '../models/task.model';

@Component({
  selector: 'app-create-task',
  standalone: true,
  imports: [ReactiveFormsModule, FormFieldComponent],
  templateUrl: './create-task.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateTaskComponent {
  readonly titleMaxLength = TITLE_MAX_LENGTH;
  readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;

  readonly isLoading = signal(false);
  readonly fieldErrors = signal<FieldErrors>({});
  readonly submitSucceeded = signal(false);

  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    title: ['', [Validators.required, notBlankValidator(), Validators.maxLength(TITLE_MAX_LENGTH)]],
    description: ['', [Validators.maxLength(DESCRIPTION_MAX_LENGTH)]],
    dueDate: ['', [futureDateValidator()]],
  });

  onSubmit(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.fieldErrors.set({});
    this.submitSucceeded.set(false);

    const request = this.buildRequest();

    this.taskService.createTask(request).subscribe({
      next: () => {
        this.form.reset({ title: '', description: '', dueDate: '' });
        this.fieldErrors.set({});
        this.submitSucceeded.set(true);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 400 && error.error) {
          this.fieldErrors.set(mapApiErrorToFieldErrors(error.error as ApiError));
        }

        this.isLoading.set(false);
      },
    });
  }

  private buildRequest(): CreateTaskRequest {
    const { title, description, dueDate } = this.form.getRawValue();
    const trimmedTitle = (title ?? '').trim();

    return {
      title: trimmedTitle,
      description: description?.trim() ? description.trim() : null,
      dueDate: dueDate ? new Date(dueDate).toISOString() : null,
    };
  }
}
