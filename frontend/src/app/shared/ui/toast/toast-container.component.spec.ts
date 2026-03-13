import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ToastContainerComponent } from './toast-container.component';
import { ToastService } from './toast.service';

describe('ToastContainerComponent', () => {
  let fixture: ComponentFixture<ToastContainerComponent>;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastContainerComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([])
      ]
    }).compileComponents();

    toastService = TestBed.inject(ToastService);
    fixture = TestBed.createComponent(ToastContainerComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should render a toast item when a toast exists', async () => {
    vi.useFakeTimers();
    toastService.success('Hello World');
    fixture.detectChanges();
    await fixture.whenStable();

    const toastItem = fixture.nativeElement.querySelector('[data-testid="toast-item"]');

    expect(toastItem).toBeTruthy();
    expect(toastItem.textContent).toContain('Hello World');
  });

  it('should dismiss toast when dismiss button is clicked', async () => {
    vi.useFakeTimers();
    toastService.success('Dismiss me');
    fixture.detectChanges();
    await fixture.whenStable();

    const dismissButton = fixture.nativeElement.querySelector('[data-testid="toast-dismiss"]');
    dismissButton.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const toastItem = fixture.nativeElement.querySelector('[data-testid="toast-item"]');
    expect(toastItem).toBeNull();
  });

  it('should apply success styling for success toast', async () => {
    vi.useFakeTimers();
    toastService.success('Success toast');
    fixture.detectChanges();
    await fixture.whenStable();

    const toastItem = fixture.nativeElement.querySelector('[data-testid="toast-item"]');

    expect(toastItem.classList.contains('bg-green-50')).toBe(true);
  });

  it('should apply error styling for error toast', async () => {
    vi.useFakeTimers();
    toastService.error('Error toast');
    fixture.detectChanges();
    await fixture.whenStable();

    const toastItem = fixture.nativeElement.querySelector('[data-testid="toast-item"]');

    expect(toastItem.classList.contains('bg-red-50')).toBe(true);
  });
});
