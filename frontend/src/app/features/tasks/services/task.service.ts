import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

export interface Task {
  id: string;
  title: string;
  description: string;
  status: 'Todo' | 'InProgress' | 'Done';
  dueDate: string;
  userId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  status: 'Todo' | 'InProgress' | 'Done';
  dueDate: string;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
  status: 'Todo' | 'InProgress' | 'Done';
  dueDate: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/tasks`;

  readonly tasks = signal<Task[]>([]);
  readonly isLoading = signal<boolean>(false);

  getAll(page = 1, pageSize = 20): void {
    this.isLoading.set(true);
    this.http.get<PagedResult<Task>>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`).subscribe({
      next: (result) => {
        this.tasks.set(result.items);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  create(req: CreateTaskRequest): void {
    this.http.post<Task>(this.apiUrl, req).subscribe({
      next: (task) => {
        this.tasks.update(tasks => [...tasks, task]);
      }
    });
  }

  update(id: string, req: UpdateTaskRequest): void {
    this.http.put<Task>(`${this.apiUrl}/${id}`, req).subscribe({
      next: (updatedTask) => {
        this.tasks.update(tasks =>
          tasks.map(t => t.id === id ? updatedTask : t)
        );
      }
    });
  }

  delete(id: string): void {
    const previous = this.tasks();
    this.tasks.update(tasks => tasks.filter(t => t.id !== id));
    this.http.delete<void>(`${this.apiUrl}/${id}`).subscribe({
      error: () => {
        this.tasks.set(previous);
      }
    });
  }
}
