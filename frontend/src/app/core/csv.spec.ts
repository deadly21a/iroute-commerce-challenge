import { describe, expect, it } from 'vitest';
import { HEADERS, hasBusinessError, isValidDate, parseCsv, validateFileName } from './csv';

const header = HEADERS.join(',') + '\n';
describe('CSV preview', () => {
  it('preserves leading zeros and quoted commas, quotes and line breaks', () => {
    const rows = parseCsv(
      '\uFEFF' + header + '2026-10-07,"Café, Sur",000123,,,"Local ""A""\nPiso 2"\n',
    );
    expect(rows[0].pc_numdoc).toBe('000123');
    expect(rows[0].pc_nomcomred).toBe('Café, Sur');
    expect(rows[0].pc_direccion).toBe('Local "A"\nPiso 2');
  });
  it('allows business errors to be imported and processed on the server', () => {
    const [row] = parseCsv(header + '2026-10-07,,A-12,,,');
    expect(row.pc_nomcomred).toBe('');
    expect(hasBusinessError(row)).toBe(true);
  });
  it.each([
    '',
    header,
    'date,name\n2026-10-07,Tienda',
    header + '2026-02-30,Tienda,123,,,',
    header + '2026-10-07,Tienda,123,,',
  ])('rejects malformed or empty CSV %s', (value) => {
    expect(() => parseCsv(value)).toThrow();
  });
  it.each([
    'commerce_31022026.csv',
    'commerce.csv',
    'commerce_07102026.txt',
    '../commerce_07102026.csv',
  ])('rejects invalid filename %s', (value) => {
    expect(() => validateFileName(value)).toThrow();
  });
  it('accepts real calendar dates including leap years', () => {
    expect(isValidDate('2024-02-29')).toBe(true);
    expect(isValidDate('2026-02-29')).toBe(false);
    expect(isValidDate('0000-01-01')).toBe(false);
    expect(isValidDate('2026-10-07')).toBe(true);
    expect(() => validateFileName('commerce_07102026.csv')).not.toThrow();
  });
});
