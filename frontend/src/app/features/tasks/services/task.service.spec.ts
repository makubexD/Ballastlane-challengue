import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TaskService } from './task.service';
import { FIXTURE_TASKS } from '../../../__fixtures__/task.fixtures';
import { environment } from '../../../../environments/environment';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TaskService]
    });
    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAll() — should fetch /api/tasks with page params and populate tasks signal', () => {
    service.getAll();

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks?page=1&pageSize=5`);
    expect(req.request.method).toBe('GET');
    req.flush({
      items: FIXTURE_TASKS,
      totalCount: FIXTURE_TASKS.length,
      page: 1,
      pageSize: 5,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false
    });

    expect(service.tasks()).toEqual(FIXTURE_TASKS);
    expect(service.isLoading()).toBe(false);
  });

  it('create() — should POST /api/tasks then reload current page via getAll()', () => {
    service.tasks.set([FIXTURE_TASKS[0]]);

    const createReq = {
      title: 'New task',
      description: 'New description',
      status: 'Todo' as const,
      dueDate: '2027-08-01T00:00:00Z'
    };

    service.create(createReq);

    const post = httpMock.expectOne(`${environment.apiUrl}/api/tasks`);
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual(createReq);

    const newTask = { ...FIXTURE_TASKS[0], id: '99', title: 'New task' };
    post.flush(newTask);

    // create() calls getAll(currentPage) → expect follow-up GET for fresh pagination
    const updatedItems = [newTask, FIXTURE_TASKS[0]];
    const get = httpMock.expectOne(`${environment.apiUrl}/api/tasks?page=1&pageSize=5`);
    get.flush({ items: updatedItems, totalCount: 2, page: 1, pageSize: 5, totalPages: 1, hasNextPage: false, hasPreviousPage: false });

    expect(service.tasks()).toEqual(updatedItems);
    expect(service.totalCount()).toBe(2);
  });

  it('delete() — should navigate to previous page when the current page becomes empty', () => {
    service.currentPage.set(2);
    service.tasks.set([FIXTURE_TASKS[0]]);

    service.delete(FIXTURE_TASKS[0].id);
    expect(service.tasks()).toHaveLength(0); // optimistic removal

    httpMock.expectOne(`${environment.apiUrl}/api/tasks/${FIXTURE_TASKS[0].id}`).flush(null);

    // tasks is empty on page > 1 → should call getAll(1)
    const get = httpMock.expectOne(`${environment.apiUrl}/api/tasks?page=1&pageSize=5`);
    get.flush({
      items: FIXTURE_TASKS,
      totalCount: FIXTURE_TASKS.length,
      page: 1,
      pageSize: 5,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false
    });

    expect(service.currentPage()).toBe(1);
    expect(service.tasks()).toEqual(FIXTURE_TASKS);
  });

  it('delete(id) — should remove task from signal immediately, refresh page, and revert on API error', () => {
    service.tasks.set([...FIXTURE_TASKS]);

    service.delete('1');

    // Optimistically removed
    expect(service.tasks().find(t => t.id === '1')).toBeUndefined();
    expect(service.tasks()).toHaveLength(2);

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    // After successful delete on page 1 with remaining tasks → getAll(1)
    const get = httpMock.expectOne(`${environment.apiUrl}/api/tasks?page=1&pageSize=5`);
    get.flush({
      items: FIXTURE_TASKS.filter(t => t.id !== '1'),
      totalCount: 2,
      page: 1,
      pageSize: 5,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false
    });

    expect(service.tasks()).toHaveLength(2);
    expect(service.tasks().find(t => t.id === '1')).toBeUndefined();
  });

  it('delete(id) — should revert optimistic removal on API error', () => {
    service.tasks.set([...FIXTURE_TASKS]);

    service.delete('1');

    // Optimistically removed
    expect(service.tasks().find(t => t.id === '1')).toBeUndefined();
    expect(service.tasks()).toHaveLength(2);

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks/1`);

    // Simulate error — should revert
    req.flush('Error', { status: 500, statusText: 'Internal Server Error' });

    expect(service.tasks()).toHaveLength(3);
    expect(service.tasks().find(t => t.id === '1')).toBeDefined();
  });

  it('should set error signal when getAll() fails', () => {
    service.getAll();

    const url = `${environment.apiUrl}/api/tasks?page=1&pageSize=5`;
    // retry(1) causes two requests: flush both with error to exhaust retries
    httpMock.expectOne(url).flush(null, { status: 500, statusText: 'Server Error' });
    httpMock.expectOne(url).flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.error()).toBe('Failed to load tasks. Please try again.');
    expect(service.isLoading()).toBe(false);
  });

  it('should populate totalCount, totalPages, hasNextPage signals on getAll() success', () => {
    service.getAll(2, 10);

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks?page=2&pageSize=10`);
    req.flush({
      items: FIXTURE_TASKS,
      totalCount: 30,
      page: 2,
      pageSize: 10,
      totalPages: 3,
      hasNextPage: true,
      hasPreviousPage: true
    });

    expect(service.totalCount()).toBe(30);
    expect(service.totalPages()).toBe(3);
    expect(service.hasNextPage()).toBe(true);
    expect(service.currentPage()).toBe(2);
  });

  it('should set createError signal when create() fails', () => {
    service.create({ title: 'Test', description: '', status: 'Todo', dueDate: '2027-01-01T00:00:00Z' });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks`);
    req.flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.createError()).toBe('Failed to create task.');
  });

  it('should set updateError signal when update() fails', () => {
    service.update('1', { title: 'Updated', description: '', status: 'Todo', dueDate: '2027-01-01T00:00:00Z' });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks/1`);
    req.flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.updateError()).toBe('Failed to update task.');
  });
});
