import { FormControl } from '@angular/forms';
import { passwordStrengthValidator } from './password-strength.validator';

const VALID_PASSWORD = 'Secure1!';
const SHORT_PASSWORD = 'Ab1';
const NO_UPPERCASE_PASSWORD = 'secure123';
const NO_NUMERIC_PASSWORD = 'SecurePass';

describe('passwordStrengthValidator', () => {
  it('should return null for a valid strong password', () => {
    const control = new FormControl(VALID_PASSWORD);

    const result = passwordStrengthValidator(control);

    expect(result).toBeNull();
  });

  it('should return minLength error for a password shorter than 8 characters', () => {
    const control = new FormControl(SHORT_PASSWORD);

    const result = passwordStrengthValidator(control);

    expect(result).toEqual({ minLength: true });
  });

  it('should return uppercase error for a password with no uppercase letter', () => {
    const control = new FormControl(NO_UPPERCASE_PASSWORD);

    const result = passwordStrengthValidator(control);

    expect(result).toEqual({ uppercase: true });
  });

  it('should return numeric error for a password with no number', () => {
    const control = new FormControl(NO_NUMERIC_PASSWORD);

    const result = passwordStrengthValidator(control);

    expect(result).toEqual({ numeric: true });
  });

  it('should return null when control value is empty', () => {
    const control = new FormControl('');

    const result = passwordStrengthValidator(control);

    expect(result).toBeNull();
  });

  it('should return null when control value is null', () => {
    const control = new FormControl(null);

    const result = passwordStrengthValidator(control);

    expect(result).toBeNull();
  });
});
