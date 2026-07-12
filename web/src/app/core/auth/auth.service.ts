import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import type { Observable } from 'rxjs';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import type {
  AuthUser,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
} from './auth.models';

const ACCESS_TOKEN_KEY = 'access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly currentUser = signal<AuthUser | null>(this.decodeUserFromToken());

  login(email: string, password: string): Observable<LoginResponse> {
    const request: LoginRequest = { email, password };

    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, request)
      .pipe(
        tap((response) => {
          this.storeToken(response.accessToken);
          this.currentUser.set(response.user);
        }),
      );
  }

  register(email: string, name: string, password: string): Observable<RegisterResponse> {
    const request: RegisterRequest = { email, name, password };

    return this.http.post<RegisterResponse>(`${environment.apiBaseUrl}/api/auth/register`, request);
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  private storeToken(token: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, token);
  }

  private decodeUserFromToken(): AuthUser | null {
    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      return {
        id: payload.id ?? payload.sub ?? '',
        email: payload.email ?? '',
        name: payload.name ?? '',
      };
    } catch {
      return null;
    }
  }
}
