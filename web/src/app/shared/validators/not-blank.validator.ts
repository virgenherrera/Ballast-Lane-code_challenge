import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Rejects values that are empty or contain only whitespace (including NBSP).
 * Complements Validators.required, which only rejects empty/null/undefined.
 */
export function notBlankValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').replace(/[\s ]+/g, '');

    return value.length === 0 ? { notBlank: true } : null;
  };
}
