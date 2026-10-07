import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/api.service';

@Component({
  imports: [ReactiveFormsModule],
  template: `
    <div class="login-page">
      <section class="login-story">
        <a class="brand light" href="/"
          ><span class="brand-icon">ir</span> iRoute<span class="brand-dot">.</span></a
        >
        <div class="login-story-content">
          <span class="eyebrow light">GESTIÓN DE COMERCIOS</span>
          <h1>La calidad de tus datos<br />empieza aquí<span>.</span></h1>
          <p>Un espacio para importar, validar y revisar<br />la información de tus comercios.</p>
          <div class="login-steps">
            <span><b>01</b> Importa tu archivo</span><span><b>02</b> Procesa por fecha</span
            ><span><b>03</b> Revisa los errores</span>
          </div>
        </div>
        <small>iRoute Commerce · Espacio de trabajo</small>
        <div class="orbit orbit-one"></div>
        <div class="orbit orbit-two"></div>
      </section>
      <section class="login-form-area">
        <div class="login-card">
          <span class="eyebrow">BIENVENIDO A TU WORKSPACE</span>
          <h2>Inicia sesión</h2>
          <p class="muted">Ingresa para gestionar tus archivos y registros.</p>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <label for="email">Correo electrónico</label
            ><input
              id="email"
              type="email"
              autocomplete="username"
              formControlName="email"
              placeholder="nombre@empresa.com"
            />
            <label for="password">Contraseña</label>
            <div class="password-field">
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                autocomplete="current-password"
                formControlName="password"
                placeholder="Ingresa tu contraseña"
              /><button
                type="button"
                (click)="showPassword.set(!showPassword())"
                [attr.aria-label]="showPassword() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
              >
                {{ showPassword() ? 'Ocultar' : 'Ver' }}
              </button>
            </div>
            @if (error()) {
              <div class="alert error" role="alert">{{ error() }}</div>
            }
            <button class="button primary full" type="submit" [disabled]="form.invalid || busy()">
              {{ busy() ? 'Ingresando…' : 'Entrar al workspace' }} <span aria-hidden="true">→</span>
            </button>
          </form>
          <div class="demo-access">
            <span class="tiny-label">ACCESO DE DEMOSTRACIÓN</span>
            <p>Explora el flujo con la cuenta de prueba.</p>
            <button class="text-button" type="button" (click)="fillDemo()">
              Usar credenciales demo <span aria-hidden="true">↗</span>
            </button>
          </div>
          <p class="login-note">Sesión protegida · Acceso al entorno de evaluación</p>
        </div>
      </section>
    </div>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  readonly busy = signal(false);
  readonly error = signal('');
  readonly showPassword = signal(false);
  fillDemo(): void {
    this.form.setValue({ email: 'demo@iroute.local', password: 'Comercio2026!' });
    this.error.set('');
  }
  submit(): void {
    if (this.form.invalid || this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    const { email, password } = this.form.getRawValue();
    this.auth
      .login(email, password)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/importar');
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
}
