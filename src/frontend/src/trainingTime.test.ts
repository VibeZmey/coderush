import { describe, expect, it } from 'vitest';
import { formatDuration, remainingSeconds } from './trainingTime';

describe('server deadline display', () => {
  it('uses elapsed time since the server response even if the client clock differs', () => {
    const server = '2026-01-01T12:00:00Z';
    const deadline = '2026-01-01T12:05:00Z';
    expect(remainingSeconds(deadline, server, 1000000, 1010000)).toBe(290);
    expect(remainingSeconds(deadline, server, 1, 300001)).toBe(0);
  });
  it('clamps expired deadlines and formats long task durations', () => {
    expect(remainingSeconds('2026-01-01T11:00:00Z', '2026-01-01T12:00:00Z', 0, 0)).toBe(0);
    expect(formatDuration(305)).toBe('5:05');
    expect(formatDuration(3600)).toBe('60:00');
  });
});
