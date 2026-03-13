import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
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

  onSubmit(): void {
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
