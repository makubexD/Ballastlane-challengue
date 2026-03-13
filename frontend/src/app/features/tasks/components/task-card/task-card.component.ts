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
    <div class="card-surface shadow-sm p-3 sm:p-4 hover:-translate-y-0.5 hover:shadow-md transition-all duration-150" data-testid="task-card">
      <h3 class="text-xs sm:text-sm font-medium text-gray-900 truncate" data-testid="task-title">
        {{ task().title }}
      </h3>
      <div class="mt-1 flex items-center justify-between gap-2">
        <p class="flex-1 min-w-0 text-xs text-gray-500 truncate" data-testid="task-due-date">
          Due: {{ task().dueDate | date:'MMM d, yyyy' }}
        </p>
        <div class="shrink-0">
          <app-badge [status]="task().status" />
        </div>
      </div>
      @if (isOverdue()) {
        <span class="inline-flex items-center mt-1 text-xs font-medium text-red-600" data-testid="overdue-badge">
          &#9888; Overdue
        </span>
      }
      <div class="mt-2 sm:mt-3 flex flex-wrap justify-end gap-2">
        @if (showDeleteConfirm()) {
          <span class="text-xs text-gray-600 self-center mr-1">Delete?</span>
          <button
            type="button"
            class="btn-sm-danger"
            data-testid="confirm-delete-button"
            (click)="confirmDelete()"
          >Confirm</button>
          <button
            type="button"
            class="btn-sm-secondary"
            data-testid="cancel-delete-button"
            (click)="cancelDelete()"
          >Cancel</button>
        } @else {
          <button
            type="button"
            class="btn-sm-ghost-brand"
            data-testid="edit-button"
            (click)="onEdit()"
          >Edit</button>
          <button
            type="button"
            class="btn-sm-ghost-danger"
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
