import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService, errorMessage } from '../core/api.service';
import { PageResult, QuarantineRow } from '../core/models';
import { isValidDate } from '../core/csv';

@Component({
  imports: [FormsModule, DatePipe, RouterLink],
  template: `
    <div class="page-heading">
      <div>
        <span class="eyebrow">PASO 03 / REVISIÓN DE ERRORES</span>
        <h1>Registros en cuarentena<span class="heading-dot">.</span></h1>
        <p>Consulta los registros separados y comprende el motivo de cada error.</p>
      </div>
      <button class="button secondary" (click)="load()" [disabled]="loading()">
        {{ loading() ? 'Actualizando…' : '↻ Actualizar' }}
      </button>
    </div>
    <div class="quarantine-banner">
      <span class="banner-symbol" aria-hidden="true">!</span>
      <div>
        <h2>La información original se conserva</h2>
        <p>
          Cada registro mantiene sus datos, el lote de origen y todos sus motivos de validación.
        </p>
      </div>
      <span class="pill warning">{{ data()?.totalCount ?? 0 }} registros</span>
    </div>
    @if (error()) {
      <div class="alert error" role="alert">{{ error() }}</div>
    }
    <section class="panel">
      <div class="filter-bar">
        <div>
          <label for="filter-date">Fecha de proceso</label
          ><input
            id="filter-date"
            type="date"
            [ngModel]="date()"
            (ngModelChange)="date.set($event)"
            [disabled]="loading()"
          />
        </div>
        <button
          class="button primary"
          (click)="applyFilter()"
          [disabled]="loading() || (!!date() && !validDate())"
        >
          Aplicar filtro</button
        ><button class="text-button" (click)="clearFilter()" [disabled]="loading()">
          Ver todas las fechas
        </button>
      </div>
      @if (loading()) {
        <div class="loading-state" role="status">
          <span class="spinner"></span> Consultando registros…
        </div>
      } @else if (data()?.items?.length) {
        <div class="table-scroll">
          <table class="quarantine-table">
            <thead>
              <tr>
                <th>Comercio / Documento</th>
                <th>Fecha de proceso</th>
                <th>Motivo de cuarentena</th>
                <th>Separado el</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (row of data()?.items; track row.id) {
                <tr>
                  <td>
                    <strong>{{ row.pcNomcomred.trim() || 'Sin nombre' }}</strong
                    ><small class="cell-secondary mono">{{
                      row.pcNumdoc || 'Sin documento'
                    }}</small>
                  </td>
                  <td class="mono">{{ row.pcProcessdate }}</td>
                  <td>
                    <div class="reason">{{ row.motivo }}</div>
                  </td>
                  <td>{{ row.quarantinedAt + 'Z' | date: 'dd/MM/yyyy HH:mm' }}</td>
                  <td>
                    <button
                      class="text-button"
                      (click)="detail.set(row)"
                      [attr.aria-label]="'Ver detalles del registro ' + row.id"
                    >
                      Detalles ↗
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      } @else {
        <div class="empty-state">
          <div class="empty-icon" aria-hidden="true">✓</div>
          <h3>No hay registros en cuarentena{{ appliedDate() ? ' para esta fecha' : '' }}</h3>
          <p>Los registros con errores aparecerán aquí después de procesar su fecha.</p>
          <a class="button secondary" routerLink="/procesar">Ir a procesar →</a>
        </div>
      }
      <div class="pagination">
        <span
          >{{ data()?.totalCount ?? 0 }} registros · Página {{ page() }} de {{ totalPages() }}</span
        >
        <div>
          <label for="page-size" class="visually-hidden">Registros por página</label
          ><select
            id="page-size"
            [ngModel]="pageSize()"
            (ngModelChange)="changeSize(+$event)"
            [disabled]="loading()"
          >
            <option [value]="10">10 por página</option>
            <option [value]="25">25 por página</option>
            <option [value]="50">50 por página</option></select
          ><button
            class="button secondary small"
            (click)="move(-1)"
            [disabled]="page() <= 1 || loading()"
            aria-label="Página anterior"
          >
            ←</button
          ><button
            class="button secondary small"
            (click)="move(1)"
            [disabled]="page() >= totalPages() || loading()"
            aria-label="Página siguiente"
          >
            →
          </button>
        </div>
      </div>
    </section>
    @if (detail(); as row) {
      <div class="modal-backdrop" (click)="detail.set(null)">
        <section
          class="detail-modal"
          role="dialog"
          aria-modal="true"
          aria-labelledby="detail-title"
          (click)="$event.stopPropagation()"
          (keydown.escape)="detail.set(null)"
          tabindex="-1"
        >
          <div class="panel-heading">
            <div>
              <span class="tiny-label">REGISTRO #{{ row.id }}</span>
              <h2 id="detail-title">Detalle de cuarentena</h2>
            </div>
            <button class="text-button" (click)="detail.set(null)" aria-label="Cerrar detalle">
              Cerrar ×
            </button>
          </div>
          <div class="panel-body">
            <dl>
              <dt>Comercio</dt>
              <dd>{{ row.pcNomcomred || 'Vacío' }}</dd>
              <dt>Documento</dt>
              <dd class="mono">{{ row.pcNumdoc || 'Vacío' }}</dd>
              <dt>Fecha de proceso</dt>
              <dd>{{ row.pcProcessdate }}</dd>
              <dt>Correo</dt>
              <dd>{{ row.pcEmail || 'Vacío' }}</dd>
              <dt>Teléfono</dt>
              <dd>{{ row.pcTelefono || 'Vacío' }}</dd>
              <dt>Dirección</dt>
              <dd>{{ row.pcDireccion || 'Vacío' }}</dd>
              <dt>Lote de origen</dt>
              <dd class="mono break-word">{{ row.batchId }}</dd>
            </dl>
            <div class="alert warning">{{ row.motivo }}</div>
          </div>
        </section>
      </div>
    }
  `,
})
export class QuarantineComponent {
  private readonly api = inject(ApiService);
  readonly date = signal(inject(ActivatedRoute).snapshot.queryParamMap.get('fecha') ?? '');
  readonly appliedDate = signal(this.date());
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly data = signal<PageResult<QuarantineRow> | null>(null);
  readonly detail = signal<QuarantineRow | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil((this.data()?.totalCount ?? 0) / this.pageSize())),
  );
  readonly validDate = computed(() => isValidDate(this.date()));
  constructor() {
    if (this.date() && !this.validDate()) {
      this.date.set('');
      this.appliedDate.set('');
    }
    this.load();
  }
  applyFilter(): void {
    if (this.date() && !this.validDate()) return;
    this.appliedDate.set(this.date());
    this.page.set(1);
    this.load();
  }
  clearFilter(): void {
    this.date.set('');
    this.applyFilter();
  }
  changeSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }
  move(delta: number): void {
    this.page.set(this.page() + delta);
    this.load();
  }
  load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.api
      .quarantine(this.appliedDate(), this.page(), this.pageSize())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (data) => this.data.set(data),
        error: (error) => {
          this.data.set(null);
          this.error.set(errorMessage(error));
        },
      });
  }
}
