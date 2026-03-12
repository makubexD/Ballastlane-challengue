import { Component, ChangeDetectionStrategy, Input } from '@angular/core';

@Component({
  selector: 'app-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
      [class]="badgeClass"
      data-testid="badge"
    >
      {{ status }}
    </span>
  `
})
export class BadgeComponent {
  @Input() status: 'Todo' | 'InProgress' | 'Done' = 'Todo';

  get badgeClass(): string {
    const classes: Record<string, string> = {
      Todo: 'bg-blue-100 text-blue-800',
      InProgress: 'bg-yellow-100 text-yellow-800',
      Done: 'bg-green-100 text-green-800'
    };
    return classes[this.status] ?? 'bg-gray-100 text-gray-800';
  }
}
