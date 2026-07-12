import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import type { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../../core/auth/auth.service';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { notBlankValidator } from '../../../shared/validators/not-blank.validator';
import type { ApiError, FieldErrors } from '../../../shared/utils/api-error-mapper';
import { mapApiErrorToFieldErrors } from '../../../shared/utils/api-error-mapper';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, FormFieldComponent, RouterLink],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  readonly isLoading = signal(false);
  readonly fieldErrors = signal<FieldErrors>({});
  readonly submitSucceeded = signal(false);

  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    name: ['', [Validators.required, notBlankValidator(), Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  onSubmit(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.fieldErrors.set({});
    this.submitSucceeded.set(false);

    const { email, name, password } = this.form.getRawValue();

    this.authService.register(email ?? '', name ?? '', password ?? '').subscribe({
      next: () => {
        this.isLoading.set(false);
        this.submitSucceeded.set(true);
        this.router.navigate(['/login'], { queryParams: { registered: 'true' } });
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);

        if (error.status === 409) {
          this.fieldErrors.set({
            email: 'An account with this email already exists.',
          });
          return;
        }

        if (error.status === 400) {
          this.fieldErrors.set(mapApiErrorToFieldErrors(error.error as ApiError));
          return;
        }
      },
    });
  }
}
