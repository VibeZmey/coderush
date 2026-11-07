import { afterEach, expect, it, vi } from 'vitest';
import { readSession, removeSession, writeSession } from './sessionStorage';

afterEach(() => vi.unstubAllGlobals());
it('keeps execution usable when browser storage access throws', () => {
  const blocked = () => { throw new DOMException('Blocked', 'SecurityError'); };
  vi.stubGlobal('sessionStorage', { getItem: blocked, setItem: blocked, removeItem: blocked });
  expect(readSession('run')).toBeNull();
  expect(() => writeSession('run', '123')).not.toThrow();
  expect(() => removeSession('run')).not.toThrow();
});
it('remembers and removes IDs when storage is available', () => {
  const values = new Map<string, string>();
  vi.stubGlobal('sessionStorage', { getItem: (key: string) => values.get(key) ?? null,
    setItem: (key: string, value: string) => values.set(key, value), removeItem: (key: string) => values.delete(key) });
  writeSession('run', '123');
  expect(readSession('run')).toBe('123');
  removeSession('run');
  expect(readSession('run')).toBeNull();
});
