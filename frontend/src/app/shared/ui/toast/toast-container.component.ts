import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 pointer-events-none max-w-xs w-full">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          role="alert"
          data-testid="toast-item"
          [attr.data-toast-type]="toast.type"
          class="pointer-events-auto flex items-start justify-between gap-3 px-4 py-3 rounded-lg shadow-lg w-full toast-enter"
          [class.bg-green-50]="toast.type === 'success'"
          [class.border-green-300]="toast.type === 'success'"
          [class.text-green-800]="toast.type === 'success'"
          [class.border]="true"
          [class.bg-red-50]="toast.type === 'error'"
          [class.border-red-300]="toast.type === 'error'"
          [class.text-red-700]="toast.type === 'error'"
          [class.bg-brand-50]="toast.type === 'info'"
          [class.border-brand-300]="toast.type === 'info'"
          [class.text-brand-700]="toast.type === 'info'"
        >
          <span class="text-sm flex-1">{{ toast.message }}</span>
          <button
            type="button"
            data-testid="toast-dismiss"
            class="shrink-0 text-current opacity-60 hover:opacity-100 transition-opacity leading-none"
            (click)="toastService.dismiss(toast.id)"
          >&times;</button>
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  readonly toastService = inject(ToastService);
}
