import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="workspace">
      <aside class="sidebar">
        <a class="brand" routerLink="/importar" aria-label="iRoute, inicio"
          ><span class="brand-icon">ir</span> iRoute<span class="brand-dot">.</span></a
        >
        <nav aria-label="Navegación principal">
          <a routerLink="/importar" routerLinkActive="active"
            ><span aria-hidden="true">↥</span> Importar archivo <small>01</small></a
          >
          <a routerLink="/procesar" routerLinkActive="active"
            ><span aria-hidden="true">◎</span> Procesar registros <small>02</small></a
          >
          <a routerLink="/cuarentena" routerLinkActive="active"
            ><span aria-hidden="true">⊞</span> Cuarentena <small>03</small></a
          >
        </nav>
      </aside>
      <div class="main-area">
        <header class="topbar">
          <div>
            <span class="status-dot"></span> Entorno local <span class="topbar-divider">/</span
            ><span class="muted">Gestión de comercios</span>
          </div>
          <div class="user-menu">
            <span class="avatar">{{ auth.session()?.user?.name?.slice(0, 1) }}</span
            ><span class="user-name">{{ auth.session()?.user?.name }}</span
            ><button class="text-button" (click)="auth.logout()">Cerrar sesión</button>
          </div>
        </header>
        <main id="main-content"><router-outlet /></main>
        <footer class="footer">iRoute Commerce <span>Importar · Validar · Revisar</span></footer>
      </div>
    </div>
  `,
})
export class ShellComponent {
  readonly auth = inject(AuthService);
}
