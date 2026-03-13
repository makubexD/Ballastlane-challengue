import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-task-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      @for (item of [1, 2, 3]; track item) {
        <div data-testid="task-skeleton-card" class="bg-white rounded-lg border border-gray-200 p-4 animate-pulse">
          <div class="h-4 bg-gray-200 rounded w-3/4 mb-3"></div>
          <div class="h-3 bg-gray-200 rounded w-1/2 mb-4"></div>
          <div class="flex gap-2 justify-end">
            <div class="h-6 bg-gray-200 rounded w-12"></div>
            <div class="h-6 bg-gray-200 rounded w-14"></div>
          </div>
        </div>
      }
    </div>
  `
})
export class TaskSkeletonComponent {}
