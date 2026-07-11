import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Rejects due dates that are not strictly in the future.
 * Mirrors the backend's exclusive `> now` semantic (a due date equal to "now" is invalid).
 * Empty values are considered valid here — pair with Validators.required if the field is mandatory.
 */
export function futureDateValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null;
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return { futureDate: true };
    }

    return date.getTime() > Date.now() ? null : { futureDate: true };
  };
}
