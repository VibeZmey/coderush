import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError, validateCode } from './api';

afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });
describe('code request boundaries', () => {
  it('checks UTF-8 byte size, not character count', () => {
    expect(validateCode('я'.repeat(32768), '')).toBeNull();
    expect(validateCode('я'.repeat(32769), '')).toContain('64 КиБ');
    expect(validateCode('print(1)', 'я'.repeat(524289))).toContain('1 МиБ');
  });
  it('rejects empty code and null characters without trimming legitimate code', () => {
    expect(validateCode('  \n', '')).toBeTruthy();
    expect(validateCode('print(1)', '\0')).toBeTruthy();
    expect(validateCode('  print(1)\n', '\n')).toBeNull();
  });
});
describe('API transport', () => {
  it('preserves code and stdin exactly and sends JSON with same-origin cookies', async () => {
    const fetch = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'run', status: 'Pending' }), { status: 202 }));
    vi.stubGlobal('fetch', fetch);
    await api.run('assignment', 71, '  print(input())\n', 'hello\n');
    const [url, options] = fetch.mock.calls[0];
    expect(url).toBe('/api/code/run');
    expect(options.credentials).toBe('same-origin');
    expect(JSON.parse(options.body)).toEqual({ matchProblemId: 'assignment', programmingLanguageId: 71, sourceCode: '  print(input())\n', standardInput: 'hello\n' });
  });
  it('handles 204 login responses without parsing JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })));
    await expect(api.login('a@example.com', 'secret', false)).resolves.toBeUndefined();
  });
  it('never displays a proxy HTML page or server exception to the user', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<h1>Internal SQL password</h1>', { status: 502 })));
    await expect(api.me()).rejects.toThrow('Сервис временно недоступен');
  });
  it('maps forbidden operations to an understandable permissions error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 403 })));
    await expect(api.logout()).rejects.toEqual(new ApiError(403, 'Для этого действия необходима роль администратора.'));
  });
});
