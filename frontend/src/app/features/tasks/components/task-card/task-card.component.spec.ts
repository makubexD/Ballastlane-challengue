import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { TaskCardComponent } from './task-card.component';
import { FIXTURE_TASKS } from '../../../../__fixtures__/task.fixtures';

describe('TaskCardComponent', () => {
  let fixture: ComponentFixture<TaskCardComponent>;
  let component: TaskCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskCardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskCardComponent);
    component = fixture.componentInstance;
    component.task = FIXTURE_TASKS[0];
    fixture.detectChanges();
  });

  it('should display task title, status, and due date', () => {
    const el = fixture.nativeElement as HTMLElement;
    const title = el.querySelector('[data-testid="task-title"]');
    const dueDate = el.querySelector('[data-testid="task-due-date"]');
    const badge = el.querySelector('[data-testid="badge"]');

    expect(title?.textContent?.trim()).toBe('Fix login bug');
    // DatePipe formats based on local timezone, accept either May 31 or Jun 1
    expect(dueDate?.textContent).toMatch(/May 31, 2027|Jun 1, 2027/);
    expect(badge?.textContent?.trim()).toBe('Todo');
  });

  it('should emit deleteTask when delete confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    const emitted: string[] = [];
    component.deleteTask.subscribe((id: string) => emitted.push(id));

    const btn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="delete-task-button"]'
    );
    btn?.click();

    expect(emitted).toEqual(['1']);
  });

  it('should emit editTask when edit button clicked', () => {
    const emitted: unknown[] = [];
    component.editTask.subscribe((t: unknown) => emitted.push(t));

    const btn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="edit-task-button"]'
    );
    btn?.click();

    expect(emitted).toEqual([FIXTURE_TASKS[0]]);
  });
});
