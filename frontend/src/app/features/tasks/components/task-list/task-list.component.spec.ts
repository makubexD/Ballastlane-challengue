import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TaskListComponent } from './task-list.component';
import { TaskService } from '../../services/task.service';
import { FIXTURE_TASKS } from '../../../../__fixtures__/task.fixtures';

describe('TaskListComponent', () => {
  let fixture: ComponentFixture<TaskListComponent>;
  let component: TaskListComponent;
  let taskService: TaskService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskListComponent, HttpClientTestingModule],
      providers: [TaskService]
    }).compileComponents();

    taskService = TestBed.inject(TaskService);
    vi.spyOn(taskService, 'getAll').mockImplementation(() => {});

    fixture = TestBed.createComponent(TaskListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    vi.clearAllMocks();
  });

  it('should render task cards for each task in the signal', () => {
    taskService.tasks.set(FIXTURE_TASKS);
    fixture.detectChanges();

    const cards = (fixture.nativeElement as HTMLElement).querySelectorAll(
      '[data-testid="task-card"]'
    );
    expect(cards).toHaveLength(3);
  });

  it('should show EmptyStateComponent when tasks signal is empty', () => {
    taskService.tasks.set([]);
    taskService.isLoading.set(false);
    fixture.detectChanges();

    const emptyState = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="empty-state"]'
    );
    expect(emptyState).toBeTruthy();
  });

  it('should show task skeleton cards while isLoading is true', () => {
    taskService.isLoading.set(true);
    fixture.detectChanges();

    const skeletonCards = (fixture.nativeElement as HTMLElement).querySelectorAll(
      '[data-testid="task-skeleton-card"]'
    );
    expect(skeletonCards.length).toBeGreaterThan(0);
  });

  it('should show error banner when taskService has an error', () => {
    taskService.error.set('Failed to load tasks.');
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="error-banner"]'
    );
    expect(banner).toBeTruthy();
  });

  it('should hide error banner after dismissal', () => {
    taskService.error.set('Failed to load tasks.');
    fixture.detectChanges();

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="dismiss-error-button"]')
      ?.click();
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="error-banner"]'
    );
    expect(banner).toBeNull();
  });

  it('should have full-width new task button on mobile', () => {
    const button = fixture.nativeElement.querySelector('[data-testid="new-task-button"]');
    expect(button).toBeTruthy();
    expect(button.classList.contains('w-full')).toBe(true);
  });
});
