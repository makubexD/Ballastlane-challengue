import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../../core/auth/auth.service';

const VALID_EMAIL = 'user@example.com';
const VALID_PASSWORD = 'SecurePass1';
const SHORT_PASSWORD = 'Ab1';
const MOCK_USER_ID = '11111111-1111-1111-1111-111111111111';
const MOCK_REGISTER_RESPONSE = { id: MOCK_USER_ID, email: VALID_EMAIL };
const MOCK_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock';
const MOCK_AUTH_RESPONSE = { token: MOCK_TOKEN, expiresAt: '2026-12-31T00:00:00Z', userId: MOCK_USER_ID };
const CONFLICT_ERROR_MESSAGE = 'Email already in use';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let component: RegisterComponent;
  let mockAuthService: { register: ReturnType<typeof vi.fn>; login: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof signal<boolean>>; token: ReturnType<typeof signal<string | null>> };
  let router: Router;

  beforeEach(async () => {
    mockAuthService = {
      register: vi.fn(),
      login: vi.fn(),
      isAuthenticated: signal(false),
      token: signal(null)
    };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should show password length error when password is too short', () => {
    component.form.controls.password.setValue(SHORT_PASSWORD);
    component.form.controls.password.markAsTouched();

    fixture.detectChanges();

    const passwordLengthError = fixture.nativeElement.querySelector('[data-testid="password-length-error"]');

    expect(passwordLengthError).toBeTruthy();
    expect(passwordLengthError.textContent).toContain('at least 8 characters');
  });

  it('should call authService.register on valid form submission', () => {
    mockAuthService.register.mockReturnValue(of(MOCK_REGISTER_RESPONSE));
    mockAuthService.login.mockReturnValue(of(MOCK_AUTH_RESPONSE));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();

    expect(mockAuthService.register).toHaveBeenCalledWith({ email: VALID_EMAIL, password: VALID_PASSWORD });
  });

  it('should show server error when registration fails', () => {
    const errorResponse = { error: { errors: [CONFLICT_ERROR_MESSAGE] } };
    mockAuthService.register.mockReturnValue(throwError(() => errorResponse));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();
    fixture.detectChanges();

    const serverError = fixture.nativeElement.querySelector('[data-testid="server-error"]');

    expect(serverError).toBeTruthy();
    expect(serverError.textContent).toContain(CONFLICT_ERROR_MESSAGE);
  });

  it('should show email required error when form submitted with empty email', () => {
    component.form.controls.email.setValue('');
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();
    fixture.detectChanges();

    const emailError = fixture.nativeElement.querySelector('[data-testid="email-error"]');

    expect(emailError).toBeTruthy();
    expect(emailError.textContent).toContain('required');
  });

  it('should show email format error when email has invalid format and form is submitted', () => {
    component.form.controls.email.setValue('notanemail');
    component.form.controls.email.markAsTouched();
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();
    fixture.detectChanges();

    const emailFormatError = fixture.nativeElement.querySelector('[data-testid="email-format-error"]');

    expect(emailFormatError).toBeTruthy();
    expect(emailFormatError.textContent).toContain('valid email');
  });

  it('should show password uppercase error when password has no uppercase letter', () => {
    component.form.controls.password.setValue('nouppercase1');
    component.form.controls.password.markAsTouched();

    fixture.detectChanges();

    const uppercaseError = fixture.nativeElement.querySelector('[data-testid="password-uppercase-error"]');

    expect(uppercaseError).toBeTruthy();
    expect(uppercaseError.textContent).toContain('uppercase');
  });

  it('should show password digit error when password has no number', () => {
    component.form.controls.password.setValue('NoDigitsHere');
    component.form.controls.password.markAsTouched();

    fixture.detectChanges();

    const numericError = fixture.nativeElement.querySelector('[data-testid="password-numeric-error"]');

    expect(numericError).toBeTruthy();
    expect(numericError.textContent).toContain('number');
  });

  it('should disable submit button when isSubmitting is true', () => {
    component.isSubmitting.set(true);

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('[data-testid="submit-button"]');

    expect(button.disabled).toBe(true);
  });

  it('should toggle password input type when visibility button is clicked', async () => {
    fixture.detectChanges();

    const passwordInput = fixture.nativeElement.querySelector('[data-testid="password-input"]');
    const toggleButton = fixture.nativeElement.querySelector('[data-testid="toggle-password-button"]');

    expect(passwordInput.type).toBe('password');

    toggleButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('text');

    toggleButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('password');
  });

  it('should navigate to /tasks after successful registration', async () => {
    mockAuthService.register.mockReturnValue(of(MOCK_REGISTER_RESPONSE));
    mockAuthService.login.mockReturnValue(of(MOCK_AUTH_RESPONSE));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();
    await fixture.whenStable();

    expect(router.navigate).toHaveBeenCalledWith(['/tasks']);
  });
});
