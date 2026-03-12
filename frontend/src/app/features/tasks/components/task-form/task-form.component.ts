import {
  Component,
  ChangeDetectionStrategy,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  inject
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Task, CreateTaskRequest, UpdateTaskRequest } from '../../services/task.service';

function futureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const todayUtc = new Date().toISOString().substring(0, 10);
  if (control.value <= todayUtc) {
    return { futureDate: true };
  }
  return null;
}

@Component({
  selector: 'app-task-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <form
      [formGroup]="form"
      (ngSubmit)="onSubmit()"
      class="bg-white rounded-lg border border-gray-200 p-4 space-y-4"
      data-testid="task-form"
    >
      <h3 class="text-sm font-semibold text-gray-900">
        {{ task ? 'Edit Task' : 'New Task' }}
      </h3>

      <div>
        <label class="block text-xs font-medium text-gray-700 mb-1" for="title">Title</label>
        <input
          id="title"
          type="text"
          formControlName="title"
          class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
          data-testid="title-input"
          placeholder="Task title"
        />
        @if (titleControl.invalid && titleControl.touched) {
          <p class="mt-1 text-xs text-red-600" data-testid="title-error">Title is required</p>
        }
      </div>

      <div>
        <label class="block text-xs font-medium text-gray-700 mb-1" for="description">Description</label>
        <textarea
          id="description"
          formControlName="description"
          class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
          data-testid="description-input"
          placeholder="Task description"
          rows="3"
        ></textarea>
        @if (descriptionControl.invalid && descriptionControl.touched) {
          <p class="mt-1 text-xs text-red-600" data-testid="description-error">Description is required</p>
        }
      </div>

      <div>
        <label class="block text-xs font-medium text-gray-700 mb-1" for="status">Status</label>
        <select
          id="status"
          formControlName="status"
          class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
          data-testid="status-select"
        >
          <option value="Todo">Todo</option>
          <option value="InProgress">In Progress</option>
          <option value="Done">Done</option>
        </select>
      </div>

      <div>
        <label class="block text-xs font-medium text-gray-700 mb-1" for="dueDate">Due Date</label>
        <input
          id="dueDate"
          type="date"
          formControlName="dueDate"
          class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
          data-testid="due-date-input"
        />
        @if (dueDateControl.invalid && dueDateControl.touched) {
          @if (dueDateControl.errors?.['required']) {
            <p class="mt-1 text-xs text-red-600" data-testid="due-date-error">Due date is required</p>
          } @else if (dueDateControl.errors?.['futureDate']) {
            <p class="mt-1 text-xs text-red-600" data-testid="due-date-error">Due date must be in the future</p>
          }
        }
      </div>

      <div class="flex justify-end gap-2">
        <button
          type="button"
          class="px-3 py-1.5 text-xs font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors"
          data-testid="cancel-button"
          (click)="formCancel.emit()"
        >
          Cancel
        </button>
        <button
          type="submit"
          class="px-3 py-1.5 text-xs font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 transition-colors"
          data-testid="submit-button"
        >
          {{ task ? 'Update' : 'Create' }}
        </button>
      </div>
    </form>
  `
})
export class TaskFormComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input() task: Task | null = null;
  @Output() formSubmit = new EventEmitter<CreateTaskRequest | UpdateTaskRequest>();
  @Output() formCancel = new EventEmitter<void>();

  form = this.fb.group({
    title: ['', Validators.required],
    description: ['', Validators.required],
    status: ['Todo' as 'Todo' | 'InProgress' | 'Done', Validators.required],
    dueDate: ['', [Validators.required, futureDateValidator]]
  });

  get titleControl() { return this.form.controls.title; }
  get descriptionControl() { return this.form.controls.description; }
  get statusControl() { return this.form.controls.status; }
  get dueDateControl() { return this.form.controls.dueDate; }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['task'] && this.task) {
      // Convert ISO string to date input format (YYYY-MM-DD)
      const dueDateValue = this.task.dueDate
        ? this.task.dueDate.substring(0, 10)
        : '';
      this.form.setValue({
        title: this.task.title,
        description: this.task.description,
        status: this.task.status,
        dueDate: dueDateValue
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.value;
    const request: CreateTaskRequest | UpdateTaskRequest = {
      title: value.title!,
      description: value.description!,
      status: value.status as 'Todo' | 'InProgress' | 'Done',
      dueDate: value.dueDate! + 'T12:00:00Z'
    };

    this.formSubmit.emit(request);

    if (!this.task) {
      this.form.reset({ title: '', description: '', status: 'Todo', dueDate: '' });
    }
  }
}
