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
