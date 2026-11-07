export function remainingSeconds(deadline: string | null, serverTime: string, receivedAt: number, now: number): number {
  if (!deadline) return 0;
  return Math.max(0, Math.ceil((Date.parse(deadline) - Date.parse(serverTime) - (now - receivedAt)) / 1000));
}

export function formatDuration(seconds: number): string {
  const whole = Math.max(0, Math.floor(seconds));
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`;
}
