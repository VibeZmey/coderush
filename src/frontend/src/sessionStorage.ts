// Remembering request IDs is optional: unavailable storage must not hide API results.
export function readSession(key: string): string | null {
  try { return sessionStorage.getItem(key); } catch { return null; }
}
export function writeSession(key: string, value: string): void {
  try { sessionStorage.setItem(key, value); } catch { /* Continue with in-memory state. */ }
}
export function removeSession(key: string): void {
  try { sessionStorage.removeItem(key); } catch { /* Storage may be blocked. */ }
}
