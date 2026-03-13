import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex justify-center items-center p-4" data-testid="spinner">
      <div [class]="spinnerClass()"></div>
    </div>
  `
})
export class SpinnerComponent {
  size = input<'sm' | 'md' | 'lg'>('md');

  readonly spinnerClass = computed(() => {
    const sizes: Record<string, string> = { sm: 'w-4 h-4', md: 'w-8 h-8', lg: 'w-12 h-12' };
    return `border-4 border-brand-500 border-t-transparent rounded-full animate-spin ${sizes[this.size()]}`;
  });
}
