import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { routes } from './app/app.routes';
import { authInterceptor } from './app/core/auth.service';

@Component({ selector: 'app-root', imports: [RouterOutlet], template: '<router-outlet />' })
class AppComponent {}
bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes), provideHttpClient(withInterceptors([authInterceptor]))],
}).catch((error) => console.error('No se pudo iniciar la aplicación.', error));
