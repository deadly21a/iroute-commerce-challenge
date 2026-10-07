import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService, errorMessage } from '../core/api.service';
import { Overview, ProcessResult } from '../core/models';
import { isValidDate } from '../core/csv';

@Component({
  imports: [FormsModule, RouterLink],
  template: `
    <div class="page-heading">
      <div>
        <span class="eyebrow">PASO 02 / VALIDACIÓN DE DATOS</span>
        <h1>Procesar registros<span class="heading-dot">.</span></h1>
        <p>Selecciona una fecha de proceso y aplica las reglas de calidad.</p>
      </div>
      <span class="pill neutral">Por fecha de proceso</span>
    </div>
    @if (error()) {
      <div class="alert error" role="alert">
        {{ error() }} <button class="text-button" (click)="load()">Reintentar consulta</button>
      </div>
    }
    <div class="process-grid">
      <section class="panel">
        <div class="panel-heading">
          <div>
            <h2>Fecha de proceso</h2>
            <p>Corresponde a la columna pc_processdate del archivo.</p>
          </div>
        </div>
        <div class="panel-body">
          <label for="process-date">Selecciona la fecha</label
          ><input
            id="process-date"
            type="date"
            [ngModel]="selectedDate()"
            (ngModelChange)="selectedDate.set($event); result.set(null)"
            [disabled]="busy()"
          />
          <div class="selected-summary">
            <strong>{{ selectedSummary()?.pendingCount ?? 0 }}</strong
            ><span>registros actualmente en commerce<br />para la fecha seleccionada</span>
          </div>
          <p class="muted">
            Los registros inválidos se moverán a cuarentena con su motivo. Los registros válidos
            permanecerán en commerce.
          </p>
          <button
            class="button primary full"
            (click)="process()"
            [disabled]="!validDate() || busy() || loading()"
          >
            {{ busy() ? 'Procesando…' : 'Procesar fecha seleccionada' }} →
          </button>
        </div>
      </section>
      <section class="rules-panel">
        <span class="tiny-label">REGLAS DE VALIDACIÓN</span>
        <h2>¿Qué revisamos?</h2>
        <div class="rule">
          <span>01</span>
          <div>
            <h3>Nombre del comercio</h3>
            <p>Debe contener al menos un carácter distinto de espacios en blanco.</p>
            <code>pc_nomcomred</code>
          </div>
        </div>
        <div class="rule">
          <span>02</span>
          <div>
            <h3>Número de documento</h3>
            <p>Es obligatorio y solo admite dígitos del 0 al 9. Conserva los ceros iniciales.</p>
            <code>pc_numdoc</code>
          </div>
        </div>
        <p class="rules-footnote">
          Si un registro incumple ambas reglas, se guardan ambos motivos.
        </p>
      </section>
    </div>
    @if (result(); as processed) {
      <section class="result-panel" role="status">
        <span class="pill ok">Proceso completado</span>
        <h2>Resultado del {{ processed.processDate }}</h2>
        <div class="result-numbers">
          <div>
            <strong>{{ processed.quarantinedCount }}</strong
            ><span>movidos a cuarentena</span>
          </div>
          <div>
            <strong>{{ processed.remainingCount }}</strong
            ><span>conservados en commerce</span>
          </div>
        </div>
        <a
          class="button secondary"
          routerLink="/cuarentena"
          [queryParams]="{ fecha: processed.processDate }"
          >Revisar motivos →</a
        >
        <p>Volver a procesar la misma fecha no duplica los registros en cuarentena.</p>
      </section>
    }
    <section class="panel">
      <div class="panel-heading">
        <div>
          <h2>Fechas disponibles</h2>
          <p>Registros agrupados por fecha, independientemente del nombre del archivo.</p>
        </div>
        <button class="text-button" (click)="load()" [disabled]="loading() || busy()">
          {{ loading() ? 'Actualizando…' : 'Actualizar ↻' }}
        </button>
      </div>
      @if (overview()?.dates?.length) {
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Fecha de proceso</th>
                <th>En commerce</th>
                <th>En cuarentena</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (date of overview()?.dates; track date.processDate) {
                <tr>
                  <td class="mono">{{ date.processDate }}</td>
                  <td>{{ date.pendingCount }}</td>
                  <td>{{ date.quarantinedCount }}</td>
                  <td class="align-right">
                    <button
                      class="text-button"
                      (click)="selectDate(date.processDate)"
                      [disabled]="busy()"
                    >
                      Seleccionar →
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      } @else {
        <div class="empty-state compact">
          <h3>{{ loading() ? 'Consultando fechas…' : 'Aún no hay registros importados' }}</h3>
          <p>Importa un CSV para empezar a procesar sus fechas.</p>
          <a routerLink="/importar" class="text-button">Ir a importar →</a>
        </div>
      }
    </section>
  `,
})
export class ProcessComponent {
  private readonly api = inject(ApiService);
  readonly overview = signal<Overview | null>(null);
  readonly selectedDate = signal('');
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly result = signal<ProcessResult | null>(null);
  readonly selectedSummary = computed(() =>
    this.overview()?.dates.find((date) => date.processDate === this.selectedDate()),
  );
  readonly validDate = computed(() => isValidDate(this.selectedDate()));
  constructor() {
    this.load();
  }
  selectDate(date: string): void {
    this.selectedDate.set(date);
    this.result.set(null);
  }
  load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.api
      .overview()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (data) => {
          this.overview.set(data);
          this.error.set('');
          if (!this.selectedDate()) this.selectedDate.set(data.dates[0]?.processDate ?? '');
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
  process(): void {
    if (!this.validDate() || this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.result.set(null);
    this.api
      .process(this.selectedDate())
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
}
