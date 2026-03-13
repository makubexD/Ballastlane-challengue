import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-center gap-2 mt-4">
      <button
        data-testid="prev-button"
        [disabled]="page() === 1"
        (click)="onPrev()"
        class="px-3 py-1.5 text-sm font-medium border border-gray-300 rounded-md disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50 transition-colors"
      >
        Previous
      </button>
      <span class="text-sm text-gray-600">Page {{ page() }}</span>
      <button
        data-testid="next-button"
        [disabled]="!hasNextPage()"
        (click)="onNext()"
        class="px-3 py-1.5 text-sm font-medium border border-gray-300 rounded-md disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50 transition-colors"
      >
        Next
      </button>
    </div>
  `
})
export class PaginationComponent {
  readonly page = input(1);
  readonly hasNextPage = input(false);
  readonly totalPages = input(1);

  readonly pageChange = output<number>();

  onPrev(): void {
    this.pageChange.emit(this.page() - 1);
  }

  onNext(): void {
    this.pageChange.emit(this.page() + 1);
  }
}
