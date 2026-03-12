import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: string;
}

export interface UserProfile {
  id: string;
  email: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'auth_token';
  private readonly _token = signal<string | null>(this.loadToken());

  readonly isAuthenticated = computed(() => this._token() !== null);
  readonly token = this._token.asReadonly();

  constructor(private readonly http: HttpClient, private readonly router: Router) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(tap(response => this.storeToken(response.token)));
  }

  register(request: RegisterRequest): Observable<{ id: string; email: string }> {
    return this.http.post<{ id: string; email: string }>(
      `${environment.apiUrl}/api/auth/register`,
      request
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    this._token.set(null);
    this.router.navigate(['/login']);
  }

  getCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${environment.apiUrl}/api/auth/me`);
  }

  private storeToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
    this._token.set(token);
  }

  private loadToken(): string | null {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length !== 3) return token;
    try {
      const payload = JSON.parse(atob(parts[1]));
      if (typeof payload.exp === 'number' && payload.exp * 1000 < Date.now()) {
        localStorage.removeItem(this.tokenKey);
        return null;
      }
      return token;
    } catch {
      localStorage.removeItem(this.tokenKey);
      return null;
    }
  }
}
