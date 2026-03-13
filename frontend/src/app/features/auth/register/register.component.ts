import { Component, ChangeDetectionStrategy, signal, computed, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { passwordStrengthValidator } from '../../../shared/validators/password-strength.validator';

@Component({
  selector: 'app-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, passwordStrengthValidator]]
  });

  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly submitted = signal(false);

  readonly passwordStrength = computed(() => {
    const pw = this.form.controls.password.value ?? '';
    let score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw)) score++;
    if (/[0-9]/.test(pw)) score++;
    if (pw.length >= 12) score++;
    return score;
  });

  readonly strengthLabel = computed(() =>
    (['', 'Weak', 'Fair', 'Good', 'Strong'] as const)[this.passwordStrength()]
  );

  readonly strengthColor = computed(() =>
    (['', 'bg-red-400', 'bg-yellow-400', 'bg-brand-400', 'bg-green-500'] as const)[this.passwordStrength()]
  );

  onSubmit(): void {
    this.submitted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.serverError.set(null);

    const { email, password } = this.form.getRawValue();
    this.authService.register({ email, password }).subscribe({
      next: () => {
        this.authService.login({ email, password }).subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.router.navigate(['/tasks']);
          },
          error: () => {
            this.isSubmitting.set(false);
            this.router.navigate(['/login']);
          }
        });
      },
      error: (err: { error?: { errors?: string[] } }) => {
        this.isSubmitting.set(false);
        this.serverError.set(err.error?.errors?.[0] ?? 'Registration failed. Please try again.');
      }
    });
  }
}
