import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center p-4 sm:p-8 lg:p-12 text-center" data-testid="empty-state">
      <div class="mb-4 text-gray-300">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"/>
        </svg>
      </div>
      <p class="text-base sm:text-lg font-medium text-gray-700 mb-1">{{ message() }}</p>
      <p class="text-sm text-gray-400 mb-5">Get started by creating your first task</p>
      @if (actionLabel()) {
        <button
          type="button"
          class="btn-primary"
          data-testid="empty-state-action"
          (click)="action.emit()"
        >
          {{ actionLabel() }}
        </button>
      }
    </div>
  `
})
export class EmptyStateComponent {
  message = input('No tasks yet.');
  actionLabel = input('Create your first task');
  action = output<void>();
}
