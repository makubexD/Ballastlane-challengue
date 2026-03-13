import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { TaskFormComponent } from './task-form.component';
import { FIXTURE_TASKS } from '../../../../__fixtures__/task.fixtures';
import { CreateTaskRequest } from '../../models/task.model';

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

    vi.clearAllMocks();
  });

  it('should show "Title is required" when title is empty and form submitted', () => {
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

  it('should show "Due date must be in the future" when past date submitted', () => {
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

  it('should emit formSubmit with correct data when form is valid', () => {
    const emitSpy = vi.spyOn(component.formSubmit, 'emit');

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

    expect(emitSpy).toHaveBeenCalledOnce();
    const submitted = emitSpy.mock.calls[0][0] as CreateTaskRequest;
    expect(submitted.title).toBe('My task');
    expect(submitted.description).toBe('My description');
    expect(submitted.status).toBe('InProgress');
    expect(submitted.dueDate).toBeTruthy();
  });

  it('should pre-populate fields when task input is provided for edit mode', async () => {
    fixture.componentRef.setInput('task', FIXTURE_TASKS[0]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.form.controls.title.value).toBe('Fix login bug');
    expect(component.form.controls.description.value).toBe('Session expires too early');
    expect(component.form.controls.status.value).toBe('Todo');
    expect(component.form.controls.dueDate.value).toBe('2027-06-01');
  });

  it('should emit formCancel when cancel button is clicked', () => {
    const emitSpy = vi.spyOn(component.formCancel, 'emit');

    const cancelBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="cancel-button"]'
    );
    cancelBtn?.click();
    fixture.detectChanges();

    expect(emitSpy).toHaveBeenCalledOnce();
  });
});
