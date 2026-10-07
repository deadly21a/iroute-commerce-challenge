import { Routes } from '@angular/router';
import { authGuard } from './core/auth.service';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'importar' },
      {
        path: 'importar',
        loadComponent: () => import('./pages/upload.component').then((m) => m.UploadComponent),
      },
      {
        path: 'procesar',
        loadComponent: () => import('./pages/process.component').then((m) => m.ProcessComponent),
      },
      {
        path: 'cuarentena',
        loadComponent: () =>
          import('./pages/quarantine.component').then((m) => m.QuarantineComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
