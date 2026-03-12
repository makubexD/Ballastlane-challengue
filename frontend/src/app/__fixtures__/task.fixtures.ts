import { Task } from '../features/tasks/services/task.service';

export const FIXTURE_TASKS: Task[] = [
  {
    id: '1',
    title: 'Fix login bug',
    description: 'Session expires too early',
    status: 'Todo',
    dueDate: '2027-06-01T00:00:00Z',
    userId: 'u1',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  },
  {
    id: '2',
    title: 'Add dark mode',
    description: 'User preference toggle',
    status: 'InProgress',
    dueDate: '2027-07-01T00:00:00Z',
    userId: 'u1',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  },
  {
    id: '3',
    title: 'Write API docs',
    description: 'OpenAPI spec',
    status: 'Done',
    dueDate: '2027-05-01T00:00:00Z',
    userId: 'u1',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  }
];
