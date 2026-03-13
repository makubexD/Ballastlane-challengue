import { AbstractControl, ValidationErrors } from '@angular/forms';

export function futureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const todayUtc = new Date().toISOString().substring(0, 10);
  if (control.value <= todayUtc) {
    return { futureDate: true };
  }
  return null;
}
