import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { retry } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Task, CreateTaskRequest, UpdateTaskRequest, PagedResult } from '../models/task.model';
import { ToastService } from '../../../shared/ui/toast/toast.service';

export type { Task, CreateTaskRequest, UpdateTaskRequest, PagedResult };

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly toastService = inject(ToastService);
  private readonly apiUrl = `${environment.apiUrl}/api/tasks`;

  readonly tasks = signal<Task[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly createError = signal<string | null>(null);
  readonly updateError = signal<string | null>(null);
  readonly totalCount = signal<number>(0);
  readonly totalPages = signal<number>(1);
  readonly hasNextPage = signal<boolean>(false);
  readonly currentPage = signal<number>(1);

  getAll(page = 1, pageSize = 5): void {
    this.isLoading.set(true);
    this.currentPage.set(page);
    this.http
      .get<PagedResult<Task>>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`)
      .pipe(retry(1))
      .subscribe({
        next: (result) => {
          this.tasks.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.hasNextPage.set(result.hasNextPage);
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set('Failed to load tasks. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  create(req: CreateTaskRequest): void {
    this.http.post<Task>(this.apiUrl, req).subscribe({
      next: () => {
        this.getAll(this.currentPage());
        this.toastService.success('Task created');
      },
      error: () => {
        this.createError.set('Failed to create task.');
        this.toastService.error('Something went wrong. Please try again.');
      }
    });
  }

  update(id: string, req: UpdateTaskRequest): void {
    this.http.put<Task>(`${this.apiUrl}/${id}`, req).subscribe({
      next: (updatedTask) => {
        this.tasks.update(tasks =>
          tasks.map(t => t.id === id ? updatedTask : t)
        );
        this.toastService.success('Task updated');
      },
      error: () => {
        this.updateError.set('Failed to update task.');
        this.toastService.error('Something went wrong. Please try again.');
      }
    });
  }

  delete(id: string): void {
    const previous = this.tasks();
    const page = this.currentPage();
    this.tasks.update(tasks => tasks.filter(t => t.id !== id));
    this.http.delete<void>(`${this.apiUrl}/${id}`).subscribe({
      next: () => {
        this.toastService.success('Task deleted');
        if (this.tasks().length === 0 && page > 1) {
          this.getAll(page - 1);
        }
      },
      error: () => {
        this.tasks.set(previous);
        this.toastService.error('Something went wrong. Please try again.');
      }
    });
  }
}
