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
    fixture.componentRef.setInput('task', FIXTURE_TASKS[0]);
    fixture.detectChanges();

    vi.clearAllMocks();
  });

  it('should display task title, status, and due date', () => {
    const el = fixture.nativeElement as HTMLElement;
    const title = el.querySelector('[data-testid="task-title"]');
    const dueDate = el.querySelector('[data-testid="task-due-date"]');
    const badge = el.querySelector('[data-testid="badge"]');

    expect(title?.textContent?.trim()).toBe('Fix login bug');
    expect(dueDate?.textContent).toMatch(/May 31, 2027|Jun 1, 2027/);
    expect(badge?.textContent?.trim()).toBe('Todo');
  });

  it('should emit deleteTask with the task id when delete is confirmed', () => {
    const emitSpy = vi.spyOn(component.deleteTask, 'emit');

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="delete-button"]')
      ?.click();
    fixture.detectChanges();

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="confirm-delete-button"]')
      ?.click();

    expect(emitSpy).toHaveBeenCalledOnce();
    expect(emitSpy).toHaveBeenCalledWith('1');
  });

  it('should not emit deleteTask when cancel button is clicked after delete', () => {
    const emitSpy = vi.spyOn(component.deleteTask, 'emit');

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="delete-button"]')
      ?.click();
    fixture.detectChanges();

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="cancel-delete-button"]')
      ?.click();

    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should emit editTask with the task when edit button is clicked', () => {
    const emitSpy = vi.spyOn(component.editTask, 'emit');

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="edit-button"]')
      ?.click();

    expect(emitSpy).toHaveBeenCalledOnce();
    expect(emitSpy).toHaveBeenCalledWith(FIXTURE_TASKS[0]);
  });
});
