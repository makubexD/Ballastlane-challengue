import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ShellComponent } from './shell.component';
import { AuthService } from '../auth/auth.service';
import { UserProfile } from '../auth/auth.model';

const MOCK_USER: UserProfile = {
  id: 'user-1',
  email: 'alice@example.com',
  createdAt: '2024-01-01T00:00:00Z'
};

describe('ShellComponent', () => {
  let fixture: ComponentFixture<ShellComponent>;
  let component: ShellComponent;

  const mockAuthService = {
    logout: vi.fn(),
    getCurrentUser: vi.fn()
  };

  beforeEach(async () => {
    mockAuthService.getCurrentUser.mockReturnValue(of(MOCK_USER));

    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    vi.clearAllMocks();
  });

  it('should render the router-outlet', () => {
    const outlet = (fixture.nativeElement as HTMLElement).querySelector('router-outlet');
    expect(outlet).toBeTruthy();
  });

  it('should render the logout button', () => {
    const btn = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="logout-button"]');
    expect(btn).toBeTruthy();
  });

  it('should call authService.logout() when logout button is clicked', () => {
    const logoutSpy = vi.spyOn(mockAuthService, 'logout');
    const btn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[data-testid="logout-button"]');
    btn!.click();
    fixture.detectChanges();
    expect(logoutSpy).toHaveBeenCalledOnce();
  });

  it('should display user email from authService.getCurrentUser()', async () => {
    mockAuthService.getCurrentUser.mockReturnValue(of(MOCK_USER));
    fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const emailEl = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="user-email"]');
    expect(emailEl?.textContent?.trim()).toBe(MOCK_USER.email);
  });

  it('should have min-w-0 on title to prevent overflow', () => {
    const spans = fixture.nativeElement.querySelectorAll('span');
    const titleSpan = Array.from(spans).find((s: any) => s.textContent.includes('Task Manager') || s.classList.contains('min-w-0'));
    expect(titleSpan).toBeTruthy();
    expect((titleSpan as HTMLElement).classList.contains('min-w-0')).toBe(true);
  });

  it('should have min-w-0 on the nav left group to allow title truncation', () => {
    const allDivs = Array.from(fixture.nativeElement.querySelectorAll('div')) as HTMLElement[];
    const navLeftGroup = allDivs.find(el => el.classList.contains('flex-1') && el.classList.contains('min-w-0'));

    expect(navLeftGroup).toBeTruthy();
    expect(navLeftGroup!.classList.contains('min-w-0')).toBe(true);
  });
});
