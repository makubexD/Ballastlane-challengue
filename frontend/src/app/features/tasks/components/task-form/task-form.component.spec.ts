import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { TaskFormComponent } from './task-form.component';
import { FIXTURE_TASKS } from '../../../../__fixtures__/task.fixtures';
import { CreateTaskRequest } from '../../services/task.service';

describe('TaskFormComponent', () => {
  let fixture: ComponentFixture<TaskFormComponent>;
  let component: TaskFormComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskFormComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should show "Title is required" when title is empty and form submitted', async () => {
    const submitBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="submit-button"]'
    );
    submitBtn?.click();
    fixture.detectChanges();

    const titleError = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="title-error"]'
    );
    expect(titleError?.textContent?.trim()).toBe('Title is required');
  });

  it('should show "Due date must be in the future" when past date submitted', async () => {
    component.form.controls.title.setValue('Test task');
    component.form.controls.description.setValue('Some description');
    component.form.controls.status.setValue('Todo');
    component.form.controls.dueDate.setValue('2020-01-01');
    component.form.controls.dueDate.markAsTouched();
    fixture.detectChanges();

    const submitBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="submit-button"]'
    );
    submitBtn?.click();
    fixture.detectChanges();

    const dueDateError = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="due-date-error"]'
    );
    expect(dueDateError?.textContent?.trim()).toBe('Due date must be in the future');
  });

  it('should emit formSubmit with correct data when form is valid', async () => {
    const emitted: unknown[] = [];
    component.formSubmit.subscribe((data: unknown) => emitted.push(data));

    component.form.controls.title.setValue('My task');
    component.form.controls.description.setValue('My description');
    component.form.controls.status.setValue('InProgress');
    component.form.controls.dueDate.setValue('2030-12-31');
    fixture.detectChanges();

    const submitBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="submit-button"]'
    );
    submitBtn?.click();
    fixture.detectChanges();

    expect(emitted).toHaveLength(1);
    const submitted = emitted[0] as CreateTaskRequest;
    expect(submitted.title).toBe('My task');
    expect(submitted.description).toBe('My description');
    expect(submitted.status).toBe('InProgress');
    expect(submitted.dueDate).toBeTruthy();
  });

  it('should pre-populate fields when task input is provided (edit mode)', async () => {
    component.task = FIXTURE_TASKS[0];
    component.ngOnChanges({ task: { currentValue: FIXTURE_TASKS[0], previousValue: null, firstChange: true, isFirstChange: () => true } });
    fixture.detectChanges();

    expect(component.form.controls.title.value).toBe('Fix login bug');
    expect(component.form.controls.description.value).toBe('Session expires too early');
    expect(component.form.controls.status.value).toBe('Todo');
    expect(component.form.controls.dueDate.value).toBe('2027-06-01');
  });
});
