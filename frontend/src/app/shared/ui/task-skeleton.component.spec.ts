import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { TaskSkeletonComponent } from './task-skeleton.component';

describe('TaskSkeletonComponent', () => {
  let fixture: ComponentFixture<TaskSkeletonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskSkeletonComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskSkeletonComponent);
    fixture.detectChanges();
  });

  it('should render 3 skeleton cards', () => {
    const cards = (fixture.nativeElement as HTMLElement).querySelectorAll(
      '[data-testid="task-skeleton-card"]'
    );
    expect(cards).toHaveLength(3);
  });
});
