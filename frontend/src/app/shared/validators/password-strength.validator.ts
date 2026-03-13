import { AbstractControl, ValidationErrors } from '@angular/forms';

export function passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string;
  if (!value) return null;
  if (value.length < 8) return { minLength: true };
  if (!/[A-Z]/.test(value)) return { uppercase: true };
  if (!/[0-9]/.test(value)) return { numeric: true };
  return null;
}
