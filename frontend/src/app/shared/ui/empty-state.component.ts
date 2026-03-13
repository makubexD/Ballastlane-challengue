import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center p-12 text-center" data-testid="empty-state">
      <p class="text-gray-500 text-lg mb-4">{{ message() }}</p>
      @if (actionLabel()) {
        <button
          type="button"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
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
