import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, ActivatedRoute, convertToParamMap } from '@angular/router';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';

const RETURN_URL = '/tasks';
const VALID_EMAIL = 'user@example.com';
const VALID_PASSWORD = 'SecurePass1';
const MOCK_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock';
const MOCK_USER_ID = '11111111-1111-1111-1111-111111111111';
const MOCK_AUTH_RESPONSE = { token: MOCK_TOKEN, expiresAt: '2026-12-31T00:00:00Z', userId: MOCK_USER_ID };
const ERROR_MESSAGE = 'Invalid credentials';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let mockAuthService: { login: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof signal<boolean>>; token: ReturnType<typeof signal<string | null>> };
  let router: Router;

  beforeEach(async () => {
    mockAuthService = {
      login: vi.fn(),
      isAuthenticated: signal(false),
      token: signal(null)
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should show email required error when form submitted with empty email', () => {
    component.form.controls.email.setValue('');
    component.form.controls.password.setValue('');

    component.onSubmit();
    fixture.detectChanges();

    const emailError = fixture.nativeElement.querySelector('[data-testid="email-error"]');

    expect(emailError).toBeTruthy();
    expect(emailError.textContent).toContain('required');
  });

  it('should show invalid email format error when email is invalid', () => {
    component.form.controls.email.setValue('notanemail');
    component.form.controls.email.markAsTouched();

    fixture.detectChanges();

    const emailFormatError = fixture.nativeElement.querySelector('[data-testid="email-format-error"]');

    expect(emailFormatError).toBeTruthy();
    expect(emailFormatError.textContent).toContain('Invalid email format');
  });

  it('should disable submit button while isSubmitting is true', () => {
    component.isSubmitting.set(true);

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('[data-testid="submit-button"]');

    expect(button.disabled).toBe(true);
  });

  it('should call authService.login and navigate to /tasks on success', () => {
    mockAuthService.login.mockReturnValue(of(MOCK_AUTH_RESPONSE));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();

    expect(mockAuthService.login).toHaveBeenCalledWith({ email: VALID_EMAIL, password: VALID_PASSWORD });
    expect(router.navigate).toHaveBeenCalledWith(['/tasks']);
  });

  it('should show server error when API returns error', () => {
    const errorResponse = { error: { errors: [ERROR_MESSAGE] } };
    mockAuthService.login.mockReturnValue(throwError(() => errorResponse));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);

    component.onSubmit();
    fixture.detectChanges();

    const serverError = fixture.nativeElement.querySelector('[data-testid="server-error"]');

    expect(serverError).toBeTruthy();
    expect(serverError.textContent).toContain(ERROR_MESSAGE);
  });

  it('should show password required error when form submitted with empty password', () => {
    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue('');

    component.onSubmit();
    fixture.detectChanges();

    const passwordError = fixture.nativeElement.querySelector('[data-testid="password-error"]');

    expect(passwordError).toBeTruthy();
    expect(passwordError.textContent.toLowerCase()).toContain('required');
  });

  it('should disable submit button when isSubmitting is true and form is submitted', async () => {
    mockAuthService.login.mockReturnValue(of(MOCK_AUTH_RESPONSE));
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

  it('should stack vertically on narrow screens', () => {
    const container = fixture.nativeElement.querySelector('div');
    expect(container.classList.contains('flex-col')).toBe(true);
  });

  it('should navigate to /tasks when no returnUrl is present', () => {
    mockAuthService.login.mockReturnValue(of(undefined));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);
    component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/tasks']);
  });
});

describe('LoginComponent — with returnUrl query param', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let mockAuthService: { login: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof signal<boolean>>; token: ReturnType<typeof signal<string | null>> };
  let router: Router;

  beforeEach(async () => {
    mockAuthService = {
      login: vi.fn(),
      isAuthenticated: signal(false),
      token: signal(null)
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ returnUrl: RETURN_URL }) } }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should show redirect notice when returnUrl query param is present', async () => {
    await fixture.whenStable();
    fixture.detectChanges();

    const notice = fixture.nativeElement.querySelector('[data-testid="redirect-notice"]');

    expect(notice).toBeTruthy();
  });

  it('should navigate to returnUrl after successful login', () => {
    mockAuthService.login.mockReturnValue(of(undefined));

    component.form.controls.email.setValue(VALID_EMAIL);
    component.form.controls.password.setValue(VALID_PASSWORD);
    component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith([RETURN_URL]);
  });
});
