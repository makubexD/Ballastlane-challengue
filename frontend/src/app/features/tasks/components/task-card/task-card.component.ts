import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Task } from '../../models/task.model';
import { BadgeComponent } from '../../../../shared/ui/badge.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, BadgeComponent],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-4" data-testid="task-card">
      <div class="flex items-start justify-between gap-2">
        <div class="flex-1 min-w-0">
          <h3 class="text-sm font-medium text-gray-900 truncate" data-testid="task-title">
            {{ task().title }}
          </h3>
          <p class="mt-1 text-xs text-gray-500" data-testid="task-due-date">
            Due: {{ task().dueDate | date:'MMM d, yyyy' }}
          </p>
        </div>
        <app-badge [status]="task().status" />
      </div>
      <div class="mt-3 flex justify-end gap-2">
        <button
          type="button"
          class="px-3 py-1 text-xs font-medium text-blue-700 bg-blue-50 rounded-md hover:bg-blue-100 transition-colors"
          data-testid="edit-task-button"
          (click)="onEdit()"
        >
          Edit
        </button>
        <button
          type="button"
          class="px-3 py-1 text-xs font-medium text-red-700 bg-red-50 rounded-md hover:bg-red-100 transition-colors"
          data-testid="delete-task-button"
          (click)="onDelete()"
        >
          Delete
        </button>
      </div>
    </div>
  `
})
export class TaskCardComponent {
  task = input.required<Task>();
  editTask = output<Task>();
  deleteTask = output<string>();

  onEdit(): void {
    this.editTask.emit(this.task());
  }

  onDelete(): void {
    if (window.confirm('Delete this task?')) {
      this.deleteTask.emit(this.task().id);
    }
  }
}
