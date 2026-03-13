import {
  Component,
  ChangeDetectionStrategy,
  inject,
  input,
  output,
  effect
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Task, CreateTaskRequest, UpdateTaskRequest } from '../../models/task.model';
import { futureDateValidator } from '../../../../shared/validators/future-date.validator';
import { DatePickerComponent } from '../../../../shared/ui/date-picker/date-picker.component';

@Component({
  selector: 'app-task-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePickerComponent],
  templateUrl: './task-form.component.html'
})
export class TaskFormComponent {
  private readonly fb = inject(FormBuilder);

  task = input<Task | null>(null);
  formSubmit = output<CreateTaskRequest | UpdateTaskRequest>();
  formCancel = output<void>();

  form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: ['', Validators.required],
    status: ['Todo' as 'Todo' | 'InProgress' | 'Done', Validators.required],
    dueDate: ['', [Validators.required, futureDateValidator]]
  });

  get titleControl() { return this.form.controls.title; }
  get descriptionControl() { return this.form.controls.description; }
  get statusControl() { return this.form.controls.status; }
  get dueDateControl() { return this.form.controls.dueDate; }

  constructor() {
    effect(() => {
      const t = this.task();
      if (t) {
        this.form.setValue({
          title: t.title,
          description: t.description ?? '',
          status: t.status,
          dueDate: t.dueDate ? t.dueDate.substring(0, 10) : ''
        });
      } else {
        this.form.reset();
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: CreateTaskRequest | UpdateTaskRequest = {
      title: value.title,
      description: value.description,
      status: value.status,
      dueDate: value.dueDate + 'T12:00:00Z'
    };

    this.formSubmit.emit(request);

    if (!this.task()) {
      this.form.reset({ title: '', description: '', status: 'Todo', dueDate: '' });
    }
  }
}
