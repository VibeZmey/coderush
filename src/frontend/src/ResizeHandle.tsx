import { useRef, type RefObject } from 'react';

export default function ResizeHandle({ container, axis, value, onChange, label, defaultValue }: {
  container: RefObject<HTMLDivElement | null>; axis: 'horizontal' | 'vertical'; value: number;
  onChange: (value: number) => void; label: string; defaultValue: number;
}) {
  const dragging = useRef(false);
  const clamp = (next: number) => Math.max(20, Math.min(80, next));
  return <div className={`workbench-divider ${axis}`} role="separator" tabIndex={0}
    aria-label={label} aria-orientation={axis === 'horizontal' ? 'vertical' : 'horizontal'}
    aria-valuemin={20} aria-valuemax={80} aria-valuenow={Math.round(value)}
    title="Перетащите для изменения размера. Двойной клик — сброс. Стрелки — настройка с клавиатуры."
    onDoubleClick={() => onChange(defaultValue)}
    onKeyDown={event => {
      const decrease = axis === 'horizontal' ? 'ArrowLeft' : 'ArrowUp';
      const increase = axis === 'horizontal' ? 'ArrowRight' : 'ArrowDown';
      if (![decrease, increase, 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      onChange(event.key === 'Home' ? 20 : event.key === 'End' ? 80 : clamp(value + (event.key === increase ? 2 : -2)));
    }}
    onPointerDown={event => { if (event.button !== 0) return; dragging.current = true; event.currentTarget.setPointerCapture(event.pointerId); event.preventDefault(); }}
    onPointerMove={event => {
      if (!dragging.current || !container.current) return;
      const rect = container.current.getBoundingClientRect();
      onChange(clamp(axis === 'horizontal' ? (event.clientX - rect.left) / rect.width * 100 : (event.clientY - rect.top) / rect.height * 100));
    }}
    onPointerUp={() => { dragging.current = false; }} onPointerCancel={() => { dragging.current = false; }}
    onLostPointerCapture={() => { dragging.current = false; }}><span /></div>;
}
