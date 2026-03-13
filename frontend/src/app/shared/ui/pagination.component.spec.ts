import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { PaginationComponent } from './pagination.component';

describe('PaginationComponent', () => {
  let fixture: ComponentFixture<PaginationComponent>;
  let component: PaginationComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginationComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(PaginationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should disable previous button on page 1', () => {
    fixture.componentRef.setInput('page', 1);
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector<HTMLButtonElement>('[data-testid="prev-button"]');
    expect(btn?.disabled).toBe(true);
  });

  it('should disable next button when hasNextPage is false', () => {
    fixture.componentRef.setInput('hasNextPage', false);
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector<HTMLButtonElement>('[data-testid="next-button"]');
    expect(btn?.disabled).toBe(true);
  });

  it('should emit pageChange with page - 1 when previous is clicked', () => {
    fixture.componentRef.setInput('page', 3);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.detectChanges();

    const emitSpy = vi.spyOn(component.pageChange, 'emit');
    fixture.nativeElement.querySelector<HTMLButtonElement>('[data-testid="prev-button"]').click();
    fixture.detectChanges();

    expect(emitSpy).toHaveBeenCalledWith(2);
    expect(emitSpy).toHaveBeenCalledOnce();
  });

  it('should emit pageChange with page + 1 when next is clicked', () => {
    fixture.componentRef.setInput('page', 2);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.detectChanges();

    const emitSpy = vi.spyOn(component.pageChange, 'emit');
    fixture.nativeElement.querySelector<HTMLButtonElement>('[data-testid="next-button"]').click();
    fixture.detectChanges();

    expect(emitSpy).toHaveBeenCalledWith(3);
    expect(emitSpy).toHaveBeenCalledOnce();
  });

  it('should display "Page X of Y" when page and totalPages inputs are set', () => {
    fixture.componentRef.setInput('page', 2);
    fixture.componentRef.setInput('totalPages', 5);
    fixture.detectChanges();

    const indicator = fixture.nativeElement.querySelector('[data-testid="page-indicator"]');
    expect(indicator?.textContent?.trim()).toBe('Page 2 of 5');
  });
});
