import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { DatePickerComponent } from './date-picker.component';

const FUTURE_DATE_ISO = '2099-06-15';
const ISO_DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

describe('DatePickerComponent', () => {
  let fixture: ComponentFixture<DatePickerComponent>;
  let component: DatePickerComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DatePickerComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(DatePickerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should open calendar panel on trigger click', async () => {
    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const panel = fixture.nativeElement.querySelector('[data-testid="calendar-panel"]');
    expect(panel).not.toBeNull();
  });

  it('should close calendar panel on Escape key', async () => {
    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const host = fixture.nativeElement as HTMLElement;
    host.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();
    await fixture.whenStable();

    const panel = fixture.nativeElement.querySelector('[data-testid="calendar-panel"]');
    expect(panel).toBeNull();
  });

  it('should navigate to next month on next arrow click', async () => {
    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const label = fixture.nativeElement.querySelector('[data-testid="month-year-label"]');
    const originalLabel = label.textContent.trim();

    fixture.nativeElement.querySelector('[data-testid="next-month-button"]').click();
    fixture.detectChanges();
    await fixture.whenStable();

    const updatedLabel = fixture.nativeElement.querySelector('[data-testid="month-year-label"]').textContent.trim();
    expect(updatedLabel).not.toBe(originalLabel);
  });

  it('should navigate to previous month on prev arrow click', async () => {
    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const label = fixture.nativeElement.querySelector('[data-testid="month-year-label"]');
    const originalLabel = label.textContent.trim();

    fixture.nativeElement.querySelector('[data-testid="prev-month-button"]').click();
    fixture.detectChanges();
    await fixture.whenStable();

    const updatedLabel = fixture.nativeElement.querySelector('[data-testid="month-year-label"]').textContent.trim();
    expect(updatedLabel).not.toBe(originalLabel);
  });

  it("should mark today's date with a special indicator", async () => {
    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const todayCell = fixture.nativeElement.querySelector('[data-testid="day-today"]');
    expect(todayCell).not.toBeNull();
  });

  it('should emit ISO date string when a future date cell is clicked', async () => {
    const onChange = vi.fn();
    component.registerOnChange(onChange);

    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const enabledDay = fixture.nativeElement.querySelector('[data-testid="day-enabled"]');
    enabledDay.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(onChange).toHaveBeenCalledOnce();
    expect(onChange).toHaveBeenCalledWith(expect.stringMatching(ISO_DATE_PATTERN));
  });

  it('should disable past date cells', async () => {
    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const disabledDay = fixture.nativeElement.querySelector('[data-testid="day-disabled"]');
    expect(disabledDay).not.toBeNull();
    const isAriaDisabled = disabledDay.getAttribute('aria-disabled') === 'true';
    const isDisabledAttr = disabledDay.hasAttribute('disabled');
    expect(isAriaDisabled || isDisabledAttr).toBe(true);
  });

  it('should call writeValue and reflect selected date when value is set externally', async () => {
    component.writeValue(FUTURE_DATE_ISO);
    fixture.detectChanges();
    await fixture.whenStable();

    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const selectedCell = fixture.nativeElement.querySelector('[data-testid="day-selected"]');
    expect(selectedCell).not.toBeNull();
  });

  it('should call onTouched when calendar is closed', async () => {
    const onTouched = vi.fn();
    component.registerOnTouched(onTouched);

    const trigger = fixture.nativeElement.querySelector('[data-testid="date-picker-trigger"]');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const host = fixture.nativeElement as HTMLElement;
    host.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(onTouched).toHaveBeenCalledOnce();
  });
});
