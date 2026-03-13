import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should add a success toast', () => {
    service.success('Task created');

    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].type).toBe('success');
    expect(service.toasts()[0].message).toBe('Task created');
  });

  it('should add an error toast', () => {
    service.error('Something went wrong');

    expect(service.toasts()[0].type).toBe('error');
  });

  it('should add an info toast', () => {
    service.info('Signed out');

    expect(service.toasts()[0].type).toBe('info');
  });

  it('should dismiss a toast by id', () => {
    service.success('Hello');
    const id = service.toasts()[0].id;

    service.dismiss(id);

    expect(service.toasts().length).toBe(0);
  });

  it('should auto-dismiss after 4000ms', () => {
    vi.useFakeTimers();
    service.success('Auto gone');
    expect(service.toasts().length).toBe(1);

    vi.advanceTimersByTime(4000);

    expect(service.toasts().length).toBe(0);
  });
});
