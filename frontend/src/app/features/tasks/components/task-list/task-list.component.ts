import { Component, ChangeDetectionStrategy, OnInit, inject, signal, computed } from '@angular/core';
import { TaskService } from '../../services/task.service';
import { Task, CreateTaskRequest, UpdateTaskRequest } from '../../models/task.model';
import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskFormComponent } from '../task-form/task-form.component';
import { TaskSkeletonComponent } from '../../../../shared/ui/task-skeleton.component';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state.component';
import { PaginationComponent } from '../../../../shared/ui/pagination.component';

@Component({
  selector: 'app-task-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TaskCardComponent, TaskFormComponent, TaskSkeletonComponent, EmptyStateComponent, PaginationComponent],
  templateUrl: './task-list.component.html'
})
export class TaskListComponent implements OnInit {
  readonly taskService = inject(TaskService);

  readonly showForm = signal<boolean>(false);
  readonly editingTask = signal<Task | null>(null);

  readonly errorMessage = computed(
    () => this.taskService.error() ?? this.taskService.createError() ?? this.taskService.updateError()
  );

  ngOnInit(): void {
    this.taskService.getAll();
  }

  dismissError(): void {
    this.taskService.error.set(null);
    this.taskService.createError.set(null);
    this.taskService.updateError.set(null);
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
