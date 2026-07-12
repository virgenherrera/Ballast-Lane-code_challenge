import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
} from './auth.models';

const ACCESS_TOKEN_KEY = 'access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  login(email: string, password: string): Observable<LoginResponse> {
    const request: LoginRequest = { email, password };

    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, request)
      .pipe(tap((response) => this.storeToken(response.accessToken)));
  }

  register(email: string, name: string, password: string): Observable<RegisterResponse> {
    const request: RegisterRequest = { email, name, password };

    return this.http.post<RegisterResponse>(`${environment.apiBaseUrl}/api/auth/register`, request);
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
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
}
