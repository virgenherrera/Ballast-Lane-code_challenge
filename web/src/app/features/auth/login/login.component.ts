import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, FormFieldComponent],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  readonly isLoading = signal(false);
  readonly loginFailed = signal(false);

  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  onSubmit(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.loginFailed.set(false);

    const { email, password } = this.form.getRawValue();

    this.authService.login(email ?? '', password ?? '').subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigateByUrl('/tasks');
      },
      error: () => {
        this.isLoading.set(false);
        this.loginFailed.set(true);
      },
    });
  }
}
