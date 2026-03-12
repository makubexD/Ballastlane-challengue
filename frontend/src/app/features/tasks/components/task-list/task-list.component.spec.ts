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
    // Prevent actual HTTP call in ngOnInit
    vi.spyOn(taskService, 'getAll').mockImplementation(() => {});

    fixture = TestBed.createComponent(TaskListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
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

  it('should show SpinnerComponent while isLoading is true', () => {
    taskService.isLoading.set(true);
    fixture.detectChanges();

    const spinner = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="spinner"]'
    );
    expect(spinner).toBeTruthy();
  });
});
