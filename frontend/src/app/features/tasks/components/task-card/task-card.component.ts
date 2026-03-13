import { Component, ChangeDetectionStrategy, input, output, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Task } from '../../models/task.model';
import { BadgeComponent } from '../../../../shared/ui/badge.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, BadgeComponent],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-3 sm:p-4 hover:-translate-y-0.5 hover:shadow-md transition-all duration-150" data-testid="task-card">
      <div class="flex items-start justify-between gap-2">
        <div class="flex-1 min-w-0">
          <h3 class="text-sm font-medium text-gray-900 truncate" data-testid="task-title">
            {{ task().title }}
          </h3>
          <p class="mt-1 text-xs text-gray-500" data-testid="task-due-date">
            Due: {{ task().dueDate | date:'MMM d, yyyy' }}
          </p>
          @if (isOverdue()) {
            <span class="inline-flex items-center mt-1 text-xs font-medium text-red-600" data-testid="overdue-badge">
              &#9888; Overdue
            </span>
          }
        </div>
        <app-badge [status]="task().status" />
      </div>
      <div class="mt-2 sm:mt-3 flex justify-end gap-2">
        @if (showDeleteConfirm()) {
          <span class="text-xs text-gray-600 self-center mr-1">Delete?</span>
          <button
            type="button"
            class="px-3 py-1 text-xs font-medium text-white bg-red-600 rounded-md hover:bg-red-700 transition-colors"
            data-testid="confirm-delete-button"
            (click)="confirmDelete()"
          >Confirm</button>
          <button
            type="button"
            class="px-3 py-1 text-xs font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors"
            data-testid="cancel-delete-button"
            (click)="cancelDelete()"
          >Cancel</button>
        } @else {
          <button
            type="button"
            class="px-3 py-1 text-xs font-medium text-brand-700 bg-brand-50 rounded-md hover:bg-brand-100 transition-colors"
            data-testid="edit-button"
            (click)="onEdit()"
          >Edit</button>
          <button
            type="button"
            class="px-3 py-1 text-xs font-medium text-red-700 bg-red-50 rounded-md hover:bg-red-100 transition-colors"
            data-testid="delete-button"
            (click)="onDelete()"
          >Delete</button>
        }
      </div>
    </div>
  `
})
export class TaskCardComponent {
  task = input.required<Task>();
  editTask = output<Task>();
  deleteTask = output<string>();

  readonly showDeleteConfirm = signal(false);

  readonly isOverdue = computed(() => {
    const t = this.task();
    if (!t.dueDate || t.status === 'Done') return false;
    return new Date(t.dueDate) < new Date();
  });

  onEdit(): void {
    this.editTask.emit(this.task());
  }

  onDelete(): void {
    this.showDeleteConfirm.set(true);
  }

  confirmDelete(): void {
    this.deleteTask.emit(this.task().id);
    this.showDeleteConfirm.set(false);
  }

  cancelDelete(): void {
    this.showDeleteConfirm.set(false);
  }
}
