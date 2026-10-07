export interface CommerceRow {
  pc_processdate: string;
  pc_nomcomred: string;
  pc_numdoc: string;
  pc_email: string;
  pc_telefono: string;
  pc_direccion: string;
}
export interface QuarantineRow {
  id: number;
  batchId: string;
  pcProcessdate: string;
  pcNomcomred: string;
  pcNumdoc: string;
  pcEmail: string;
  pcTelefono: string;
  pcDireccion: string;
  motivo: string;
  quarantinedAt: string;
}
export interface ImportResult {
  batchId: string;
  fileName: string;
  insertedCount: number;
  importedAt: string;
}
export interface ProcessResult {
  processDate: string;
  quarantinedCount: number;
  remainingCount: number;
}
export interface PageResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export interface DateSummary {
  processDate: string;
  pendingCount: number;
  quarantinedCount: number;
}
export interface Overview {
  commerceCount: number;
  quarantineCount: number;
  importCount: number;
  dates: DateSummary[];
  recentImports: { batchId: string; fileName: string; rowCount: number; importedAt: string }[];
}
export interface LoginResult {
  token: string;
  expiresAt: string;
  user: { email: string; name: string };
}
