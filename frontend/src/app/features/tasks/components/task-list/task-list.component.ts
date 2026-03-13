import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { TaskService } from '../../services/task.service';
import { Task, CreateTaskRequest, UpdateTaskRequest } from '../../models/task.model';
import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskFormComponent } from '../task-form/task-form.component';
import { SpinnerComponent } from '../../../../shared/ui/spinner.component';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state.component';

@Component({
  selector: 'app-task-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TaskCardComponent, TaskFormComponent, SpinnerComponent, EmptyStateComponent],
  templateUrl: './task-list.component.html'
})
export class TaskListComponent implements OnInit {
  readonly taskService = inject(TaskService);

  readonly showForm = signal<boolean>(false);
  readonly editingTask = signal<Task | null>(null);

  ngOnInit(): void {
    this.taskService.getAll();
  }

  onCreateSubmit(req: CreateTaskRequest | UpdateTaskRequest): void {
    this.taskService.create(req as CreateTaskRequest);
    this.showForm.set(false);
  }

  onEditSubmit(req: CreateTaskRequest | UpdateTaskRequest): void {
    const task = this.editingTask();
    if (task) {
      this.taskService.update(task.id, req as UpdateTaskRequest);
      this.editingTask.set(null);
    }
  }

  onEditTask(task: Task): void {
    this.editingTask.set(task);
    this.showForm.set(false);
  }

  onDeleteTask(id: string): void {
    this.taskService.delete(id);
  }
}
