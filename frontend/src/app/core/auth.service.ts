import { inject, Injectable, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HttpClient, HttpInterceptorFn } from '@angular/common/http';
import { catchError, tap, throwError } from 'rxjs';
import { LoginResult } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly session = signal<LoginResult | null>(this.restore());
  private restore(): LoginResult | null {
    try {
      const raw = sessionStorage.getItem('iroute-session');
      const value = raw ? (JSON.parse(raw) as LoginResult) : null;
      return value?.token && new Date(value.expiresAt).getTime() > Date.now() ? value : null;
    } catch {
      return null;
    }
  }
  isAuthenticated(): boolean {
    const session = this.session();
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  }
  login(email: string, password: string) {
    return this.http.post<LoginResult>('/api/auth/login', { email, password }).pipe(
      tap((result) => {
        this.session.set(result);
        sessionStorage.setItem('iroute-session', JSON.stringify(result));
      }),
    );
  }
  logout(): void {
    this.session.set(null);
    sessionStorage.removeItem('iroute-session');
    void this.router.navigateByUrl('/login');
  }
}
export const authGuard: CanActivateFn = () =>
  inject(AuthService).isAuthenticated() || inject(Router).createUrlTree(['/login']);
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.session()?.token;
  const protectedApi = request.url.startsWith('/api/commerce');
  const authenticated =
    protectedApi && token
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;
  return next(authenticated).pipe(
    catchError((error) => {
      if (error.status === 401 && protectedApi) auth.logout();
      return throwError(() => error);
    }),
  );
};
