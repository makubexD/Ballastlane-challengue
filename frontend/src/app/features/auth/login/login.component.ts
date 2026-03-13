import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly returnUrl = signal<string | null>(null);

  ngOnInit(): void {
    this.returnUrl.set(this.route.snapshot.queryParamMap.get('returnUrl'));
  }

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
        this.router.navigate([this.returnUrl() ?? '/tasks']);
      },
      error: (err: { error?: { errors?: string[] } }) => {
        this.isSubmitting.set(false);
        this.serverError.set(err.error?.errors?.[0] ?? 'Login failed. Please try again.');
      }
    });
  }
}
