import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  template: `
    <nav class="sticky top-0 z-10 bg-white border-b border-gray-200 shadow-sm">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex items-center justify-between h-14">
        <span class="text-lg font-semibold text-gray-900 min-w-0 truncate">Task Manager</span>
        <div class="flex items-center gap-4">
          <span data-testid="user-email" class="hidden sm:block text-sm text-gray-600">{{ userEmail() }}</span>
          <button
            data-testid="logout-button"
            (click)="logout()"
            class="hidden sm:flex px-3 py-1.5 text-sm font-medium text-brand-600 hover:text-brand-700 border border-brand-200 rounded-md hover:bg-brand-50 transition-colors"
          >
            Logout
          </button>
          <button
            data-testid="logout-button-mobile"
            type="button"
            (click)="logout()"
            class="sm:hidden p-1.5 text-brand-600 hover:text-brand-700 border border-brand-200 rounded-md hover:bg-brand-50 transition-colors"
            aria-label="Logout"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
          </button>
        </div>
      </div>
    </nav>
    <router-outlet />
  `
})
export class ShellComponent {
  private readonly authService = inject(AuthService);

  readonly userEmail = signal<string>('');

  constructor() {
    this.authService.getCurrentUser().pipe(takeUntilDestroyed()).subscribe({
      next: user => this.userEmail.set(user.email)
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
