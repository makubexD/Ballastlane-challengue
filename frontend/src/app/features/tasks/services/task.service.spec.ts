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

  it('getAll() — should fetch /api/tasks and populate tasks signal', () => {
    service.getAll();

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks`);
    expect(req.request.method).toBe('GET');
    req.flush(FIXTURE_TASKS);

    expect(service.tasks()).toEqual(FIXTURE_TASKS);
    expect(service.isLoading()).toBe(false);
  });

  it('create() — should POST /api/tasks and append task to signal', () => {
    service.tasks.set([FIXTURE_TASKS[0]]);

    const createReq = {
      title: 'New task',
      description: 'New description',
      status: 'Todo' as const,
      dueDate: '2027-08-01T00:00:00Z'
    };

    service.create(createReq);

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);

    const newTask = { ...FIXTURE_TASKS[0], id: '99', title: 'New task' };
    req.flush(newTask);

    expect(service.tasks()).toHaveLength(2);
    expect(service.tasks()[1]).toEqual(newTask);
  });

  it('delete(id) — should remove task from signal immediately and revert on API error', () => {
    service.tasks.set([...FIXTURE_TASKS]);

    service.delete('1');

    // Optimistically removed
    expect(service.tasks().find(t => t.id === '1')).toBeUndefined();
    expect(service.tasks()).toHaveLength(2);

    const req = httpMock.expectOne(`${environment.apiUrl}/api/tasks/1`);
    expect(req.request.method).toBe('DELETE');

    // Simulate error — should revert
    req.flush('Error', { status: 500, statusText: 'Internal Server Error' });

    expect(service.tasks()).toHaveLength(3);
    expect(service.tasks().find(t => t.id === '1')).toBeDefined();
  });
});
