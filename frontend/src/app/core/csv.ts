import Papa from 'papaparse';
import { CommerceRow } from './models';

export const HEADERS = [
  'pc_processdate',
  'pc_nomcomred',
  'pc_numdoc',
  'pc_email',
  'pc_telefono',
  'pc_direccion',
] as const;
export const MAX_BYTES = 10 * 1024 * 1024;
export function isValidDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || value.startsWith('0000')) return false;
  const date = new Date(value + 'T00:00:00Z');
  return !Number.isNaN(date.getTime()) && date.toISOString().slice(0, 10) === value;
}
export function validateFileName(name: string): void {
  const match = /^commerce_(\d{2})(\d{2})(\d{4})\.csv$/.exec(name);
  if (!match || !isValidDate(`${match[3]}-${match[2]}-${match[1]}`))
    throw new Error('Usa el nombre commerce_DDMMYYYY.csv con una fecha válida.');
}
export function parseCsv(text: string): CommerceRow[] {
  const parsed = Papa.parse<string[]>(text.replace(/^\uFEFF/, ''), {
    delimiter: ',',
    skipEmptyLines: true,
  });
  if (parsed.errors.length) throw new Error('CSV inválido. Revisa las comillas y separadores.');
  const [headers, ...data] = parsed.data;
  if (
    !headers ||
    headers.length !== HEADERS.length ||
    headers.some((value, i) => value !== HEADERS[i])
  )
    throw new Error(`Encabezados esperados: ${HEADERS.join(',')}.`);
  if (!data.length) throw new Error('El archivo debe contener registros además del encabezado.');
  if (data.length > 50_000) throw new Error('Máximo 50 000 registros por archivo.');
  const limits = [10, 200, 50, 254, 40, 300];
  return data.map((cells, index) => {
    if (cells.length !== HEADERS.length)
      throw new Error(`Fila ${index + 2}: se esperan seis columnas.`);
    if (!isValidDate(cells[0]))
      throw new Error(`Fila ${index + 2}: la fecha debe tener formato yyyy-MM-dd y ser válida.`);
    cells.forEach((value, i) => {
      if (value.length > limits[i])
        throw new Error(`Fila ${index + 2}: ${HEADERS[i]} supera ${limits[i]} caracteres.`);
    });
    return Object.fromEntries(HEADERS.map((name, i) => [name, cells[i]])) as unknown as CommerceRow;
  });
}
export function hasBusinessError(row: CommerceRow): boolean {
  return !row.pc_nomcomred.trim() || !/^\d+$/.test(row.pc_numdoc);
}
