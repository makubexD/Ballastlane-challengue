import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-center justify-center gap-2 mt-4">
      <button
        data-testid="prev-button"
        [disabled]="page() === 1"
        (click)="onPrev()"
        class="btn-nav"
      >
        Previous
      </button>
      <span data-testid="page-indicator" class="text-sm text-gray-600">Page {{ page() }} of {{ totalPages() }}</span>
      <button
        data-testid="next-button"
        [disabled]="!hasNextPage()"
        (click)="onNext()"
        class="btn-nav"
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
