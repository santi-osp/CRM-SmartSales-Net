import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth/auth.service';
import { LoginResponse } from './auth/auth.models';

describe('AuthService', () => {
  beforeEach(() => TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting()]
  }));

  it('mantiene la sesión en memoria y la elimina al salir', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const response: LoginResponse = {
      accessToken: 'token-de-prueba', tokenType: 'Bearer', expiresIn: 900,
      user: { id: 'user-id', displayName: 'Comercial Demo', role: 'Commercial', isActive: true }
    };

    auth.login('demo@example.test', 'password-de-prueba').subscribe();
    const request = http.expectOne('/api/contacts/v1/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'demo@example.test', password: 'password-de-prueba' });
    request.flush(response);

    expect(auth.token()).toBe('token-de-prueba');
    expect(auth.user()?.role).toBe('Commercial');
    auth.clear();
    expect(auth.token()).toBeNull();
    expect(auth.user()).toBeNull();
    http.verify();
  });
});
