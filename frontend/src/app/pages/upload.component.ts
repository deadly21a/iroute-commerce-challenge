import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService, errorMessage } from '../core/api.service';
import { CommerceRow, ImportResult, Overview } from '../core/models';
import { hasBusinessError, MAX_BYTES, parseCsv, validateFileName } from '../core/csv';

@Component({
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './upload.component.html',
})
export class UploadComponent {
  private readonly api = inject(ApiService);
  readonly overview = signal<Overview | null>(null);
  readonly summaryError = signal('');
  readonly file = signal<File | null>(null);
  readonly rows = signal<CommerceRow[]>([]);
  readonly busy = signal(false);
  readonly reading = signal(false);
  readonly dragging = signal(false);
  readonly error = signal('');
  readonly result = signal<ImportResult | null>(null);
  readonly preview = computed(() => this.rows().slice(0, 50));
  readonly warnings = computed(() => this.rows().filter(hasBusinessError).length);
  readonly dates = computed(() => new Set(this.rows().map((row) => row.pc_processdate)).size);
  readonly hasBusinessError = hasBusinessError;
  constructor() {
    this.refreshOverview();
  }
  refreshOverview(): void {
    this.api.overview().subscribe({
      next: (data) => {
        this.overview.set(data);
        this.summaryError.set('');
      },
      error: (error) => this.summaryError.set(errorMessage(error)),
    });
  }
  onSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) void this.select(file);
    input.value = '';
  }
  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    if (this.busy() || this.reading()) return;
    if (event.dataTransfer?.files.length !== 1) {
      this.error.set('Selecciona un solo archivo CSV.');
      return;
    }
    void this.select(event.dataTransfer.files[0]);
  }
  async select(file: File): Promise<void> {
    if (this.busy() || this.reading()) return;
    this.clear();
    this.reading.set(true);
    try {
      validateFileName(file.name);
      if (!file.size) throw new Error('El archivo está vacío.');
      if (file.size > MAX_BYTES) throw new Error('El archivo supera el límite de 10 MB.');
      const text = new TextDecoder('utf-8', { fatal: true }).decode(await file.arrayBuffer());
      const rows = parseCsv(text);
      this.file.set(file);
      this.rows.set(rows);
    } catch (error) {
      this.error.set(
        error instanceof TypeError
          ? 'El archivo debe estar codificado en UTF-8.'
          : errorMessage(error),
      );
    } finally {
      this.reading.set(false);
    }
  }
  clear(): void {
    this.file.set(null);
    this.rows.set([]);
    this.error.set('');
    this.result.set(null);
  }
  submit(): void {
    const file = this.file();
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .import(file)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.file.set(null);
          this.rows.set([]);
          this.refreshOverview();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
}
