import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50">
      <div class="max-w-md w-full space-y-8 p-8 bg-white rounded-xl shadow-lg">
        <h1 class="text-2xl font-bold text-center text-gray-900">Sign in</h1>

        @if (serverError()) {
          <div data-testid="server-error" class="bg-red-50 border border-red-300 text-red-700 px-4 py-3 rounded">
            {{ serverError() }}
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4" data-testid="login-form">
          <div>
            <label class="block text-sm font-medium text-gray-700">Email</label>
            <input
              data-testid="email-input"
              type="email"
              formControlName="email"
              class="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
              placeholder="you@example.com"
            />
            @if (form.controls.email.touched && form.controls.email.errors?.['required']) {
              <p data-testid="email-error" class="mt-1 text-sm text-red-600">Email is required</p>
            }
            @if (form.controls.email.touched && form.controls.email.errors?.['email']) {
              <p data-testid="email-format-error" class="mt-1 text-sm text-red-600">Invalid email format</p>
            }
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Password</label>
            <input
              data-testid="password-input"
              type="password"
              formControlName="password"
              class="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
            />
            @if (form.controls.password.touched && form.controls.password.errors?.['required']) {
              <p data-testid="password-error" class="mt-1 text-sm text-red-600">Password is required</p>
            }
          </div>

          <button
            data-testid="submit-button"
            type="submit"
            [disabled]="isSubmitting()"
            class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50"
          >
            @if (isSubmitting()) { Signing in... } @else { Sign in }
          </button>
        </form>

        <p class="text-center text-sm text-gray-600">
          Don't have an account?
          <a data-testid="register-link" routerLink="/register" class="text-indigo-600 hover:underline">Create account</a>
        </p>
      </div>
    </div>
  `
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
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
    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/tasks']);
      },
      error: (err: { error?: { errors?: string[] } }) => {
        this.isSubmitting.set(false);
        this.serverError.set(err.error?.errors?.[0] ?? 'Login failed. Please try again.');
      }
    });
  }
}
