import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

function passwordStrengthValidator(control: AbstractControl): { [key: string]: boolean } | null {
  const value = control.value as string;
  if (!value) return null;
  if (value.length < 8) return { minLength: true };
  if (!/[A-Z]/.test(value)) return { uppercase: true };
  if (!/[0-9]/.test(value)) return { numeric: true };
  return null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50">
      <div class="max-w-md w-full space-y-8 p-8 bg-white rounded-xl shadow-lg">
        <h1 class="text-2xl font-bold text-center text-gray-900">Create account</h1>

        @if (serverError()) {
          <div data-testid="server-error" class="bg-red-50 border border-red-300 text-red-700 px-4 py-3 rounded">
            {{ serverError() }}
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700">Email</label>
            <input data-testid="email-input" type="email" formControlName="email"
              class="mt-1 block w-full rounded-md border-gray-300 shadow-sm" />
            @if (form.controls.email.touched && form.controls.email.errors?.['required']) {
              <p data-testid="email-error" class="mt-1 text-sm text-red-600">Email is required</p>
            }
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Password</label>
            <input data-testid="password-input" type="password" formControlName="password"
              class="mt-1 block w-full rounded-md border-gray-300 shadow-sm" />
            @if (form.controls.password.touched && form.controls.password.errors?.['minLength']) {
              <p data-testid="password-length-error" class="mt-1 text-sm text-red-600">Password must be at least 8 characters</p>
            }
            @if (form.controls.password.touched && form.controls.password.errors?.['uppercase']) {
              <p data-testid="password-uppercase-error" class="mt-1 text-sm text-red-600">Password must contain at least one uppercase letter</p>
            }
            @if (form.controls.password.touched && form.controls.password.errors?.['numeric']) {
              <p data-testid="password-numeric-error" class="mt-1 text-sm text-red-600">Password must contain at least one number</p>
            }
          </div>

          <button data-testid="submit-button" type="submit" [disabled]="isSubmitting()"
            class="w-full py-2 px-4 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50">
            @if (isSubmitting()) { Creating account... } @else { Create account }
          </button>
        </form>

        <p class="text-center text-sm text-gray-600">
          Already have an account?
          <a data-testid="login-link" routerLink="/login" class="text-indigo-600 hover:underline">Sign in</a>
        </p>
      </div>
    </div>
  `
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
