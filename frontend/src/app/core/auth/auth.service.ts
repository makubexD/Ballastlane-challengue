import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, UserProfile } from './auth.model';

export type { LoginRequest, RegisterRequest, UserProfile };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _isAuthenticated = signal<boolean>(false);
  readonly isAuthenticated = this._isAuthenticated.asReadonly();

  initialize(): Observable<void> {
    return this.http
      .get<UserProfile>(`${environment.apiUrl}/api/auth/me`, { withCredentials: true })
      .pipe(
        tap(() => this._isAuthenticated.set(true)),
        catchError(() => {
          this._isAuthenticated.set(false);
          return of(undefined as unknown as void);
        })
      ) as Observable<void>;
  }

  login(request: LoginRequest): Observable<void> {
    return this.http
      .post<void>(`${environment.apiUrl}/api/auth/login`, request, { withCredentials: true })
      .pipe(tap(() => this._isAuthenticated.set(true)));
  }

  register(request: RegisterRequest): Observable<{ id: string; email: string }> {
    return this.http.post<{ id: string; email: string }>(
      `${environment.apiUrl}/api/auth/register`,
      request,
      { withCredentials: true }
    );
  }

  logout(): void {
    this.http
      .post(`${environment.apiUrl}/api/auth/logout`, {}, { withCredentials: true })
      .subscribe({
        complete: () => {
          this._isAuthenticated.set(false);
          this.router.navigate(['/login']);
        },
        error: () => {
          this._isAuthenticated.set(false);
          this.router.navigate(['/login']);
        }
      });
  }

  getCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${environment.apiUrl}/api/auth/me`, {
      withCredentials: true
    });
  }
}
