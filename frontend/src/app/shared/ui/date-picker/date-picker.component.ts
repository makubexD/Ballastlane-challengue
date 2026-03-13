import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  forwardRef,
  HostListener
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface CalendarDay {
  date: Date;
  dayOfMonth: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  isDisabled: boolean;
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './date-picker.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DatePickerComponent),
      multi: true
    }
  ]
})
export class DatePickerComponent implements ControlValueAccessor {
  isOpen = signal(false);
  displayMonth = signal(new Date());
  selectedDate = signal<Date | null>(null);

  private onChange: (val: string) => void = () => {};
  private onTouched: () => void = () => {};

  monthYearLabel = computed(() => {
    const d = this.displayMonth();
    return d.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  });

  daysInGrid = computed((): CalendarDay[] => {
    const display = this.displayMonth();
    const selected = this.selectedDate();

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const year = display.getFullYear();
    const month = display.getMonth();

    const firstOfMonth = new Date(year, month, 1);

    // Start on Sunday (0). Adjust so grid starts on Sunday.
    const startDay = firstOfMonth.getDay(); // 0=Sun, 1=Mon...
    const firstCell = new Date(firstOfMonth);
    firstCell.setDate(firstOfMonth.getDate() - startDay);

    const days: CalendarDay[] = [];
    const totalCells = 42;

    for (let i = 0; i < totalCells; i++) {
      const cellDate = new Date(firstCell);
      cellDate.setDate(firstCell.getDate() + i);
      cellDate.setHours(0, 0, 0, 0);

      const isCurrentMonth = cellDate.getMonth() === month && cellDate.getFullYear() === year;

      const isToday =
        cellDate.getFullYear() === today.getFullYear() &&
        cellDate.getMonth() === today.getMonth() &&
        cellDate.getDate() === today.getDate();

      const isSelected =
        selected !== null &&
        cellDate.getFullYear() === selected.getFullYear() &&
        cellDate.getMonth() === selected.getMonth() &&
        cellDate.getDate() === selected.getDate();

      // Disabled: today and past, or outside current month
      const isDisabled = cellDate <= today || !isCurrentMonth;

      days.push({
        date: cellDate,
        dayOfMonth: cellDate.getDate(),
        isCurrentMonth,
        isToday,
        isSelected,
        isDisabled
      });
    }

    return days;
  });

  readonly weekdays = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

  displayValue = computed(() => {
    const d = this.selectedDate();
    if (!d) return '';
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  });

  open(): void {
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
    this.onTouched();
  }

  toggleOpen(): void {
    if (this.isOpen()) {
      this.close();
    } else {
      this.open();
    }
  }

  prevMonth(): void {
    const d = this.displayMonth();
    this.displayMonth.set(new Date(d.getFullYear(), d.getMonth() - 1, 1));
  }

  nextMonth(): void {
    const d = this.displayMonth();
    this.displayMonth.set(new Date(d.getFullYear(), d.getMonth() + 1, 1));
  }

  selectDay(day: CalendarDay): void {
    if (day.isDisabled) return;
    this.selectedDate.set(day.date);

    const iso = this.toIso(day.date);
    this.onChange(iso);
    this.close();
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.close();
    }
  }

  writeValue(val: string | null): void {
    if (val) {
      const d = new Date(val + 'T12:00:00');
      this.selectedDate.set(d);
      this.displayMonth.set(new Date(d.getFullYear(), d.getMonth(), 1));
    } else {
      this.selectedDate.set(null);
    }
  }

  registerOnChange(fn: (val: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  getDayTestId(day: CalendarDay): string {
    if (day.isSelected) return 'day-selected';
    if (day.isToday) return 'day-today';
    if (day.isDisabled) return 'day-disabled';
    return 'day-enabled';
  }

  getDayClass(day: CalendarDay): string {
    const base = 'h-8 w-8 flex items-center justify-center text-xs rounded-full transition-colors';

    if (!day.isCurrentMonth) {
      return `${base} text-gray-300 cursor-not-allowed`;
    }

    if (day.isSelected) {
      return `${base} bg-brand-500 text-white hover:bg-brand-600 cursor-pointer`;
    }

    if (day.isDisabled) {
      return `${base} text-gray-300 cursor-not-allowed hover:bg-transparent`;
    }

    const todayRing = day.isToday ? ' ring-1 ring-brand-500' : '';
    return `${base} cursor-pointer hover:bg-brand-100${todayRing}`;
  }

  private toIso(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
