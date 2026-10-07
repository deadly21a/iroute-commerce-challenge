import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { ImportResult, Overview, PageResult, ProcessResult, QuarantineRow } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  overview() {
    return this.http.get<Overview>('/api/commerce/overview');
  }
  import(file: File) {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<ImportResult>('/api/commerce/import', body);
  }
  process(processDate: string) {
    return this.http.post<ProcessResult>('/api/commerce/process', { processDate });
  }
  quarantine(processDate: string, page: number, pageSize: number) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (processDate) params = params.set('processDate', processDate);
    return this.http.get<PageResult<QuarantineRow>>('/api/commerce/quarantine', { params });
  }
}
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0 || error.status === 502 || error.status === 504)
      return 'No se pudo conectar con el servidor. Comprueba que la API esté en ejecución.';
    if (error.status === 401)
      return error.error?.detail || 'La sesión expiró. Inicia sesión nuevamente.';
    return error.error?.detail || error.error?.title || 'No se pudo completar la operación.';
  }
  return error instanceof Error ? error.message : 'No se pudo completar la operación.';
}
