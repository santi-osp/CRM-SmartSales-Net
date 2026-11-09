import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { LoginResponse, UserSummary } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenState = signal<string | null>(null);
  private readonly userState = signal<UserSummary | null>(null);
  private expiresAt = 0;
  private expiryTimer: ReturnType<typeof setTimeout> | null = null;

  readonly user = this.userState.asReadonly();
  readonly isAuthenticated = computed(() => this.tokenState() !== null);

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/contacts/v1/auth/login', { email, password }).pipe(
      tap(response => {
        this.clear();
        this.expiresAt = Date.now() + response.expiresIn * 1000;
        this.tokenState.set(response.accessToken);
        this.userState.set(response.user);
        this.expiryTimer = setTimeout(() => this.clear(), response.expiresIn * 1000);
      })
    );
  }

  token(): string | null {
    if (Date.now() >= this.expiresAt) {
      this.clear();
      return null;
    }
    return this.tokenState();
  }

  clear(): void {
    if (this.expiryTimer !== null) clearTimeout(this.expiryTimer);
    this.expiryTimer = null;
    this.expiresAt = 0;
    this.tokenState.set(null);
    this.userState.set(null);
  }
}
