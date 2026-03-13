import { FormControl } from '@angular/forms';
import { futureDateValidator } from './future-date.validator';

const PAST_DATE = '2020-01-01';
const TODAY_DATE = new Date().toISOString().substring(0, 10);
const FUTURE_DATE = '2099-12-31';

describe('futureDateValidator', () => {
  it('should return null for a valid future date', () => {
    const control = new FormControl(FUTURE_DATE);

    const result = futureDateValidator(control);

    expect(result).toBeNull();
  });

  it('should return futureDate error for a past date', () => {
    const control = new FormControl(PAST_DATE);

    const result = futureDateValidator(control);

    expect(result).toEqual({ futureDate: true });
  });

  it('should return futureDate error for today\'s date', () => {
    const control = new FormControl(TODAY_DATE);

    const result = futureDateValidator(control);

    expect(result).toEqual({ futureDate: true });
  });

  it('should return null when control value is empty', () => {
    const control = new FormControl('');

    const result = futureDateValidator(control);

    expect(result).toBeNull();
  });

  it('should return null when control value is null', () => {
    const control = new FormControl(null);

    const result = futureDateValidator(control);

    expect(result).toBeNull();
  });

  it('should return null for a date that is exactly tomorrow', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-15T12:00:00Z'));

    const control = new FormControl('2026-01-16');

    const result = futureDateValidator(control);

    expect(result).toBeNull();

    vi.useRealTimers();
  });

  it('should return null for a non-date string that sorts after today lexicographically', () => {
    const control = new FormControl('not-a-date');

    const result = futureDateValidator(control);

    expect(result).toBeNull();
  });
});
