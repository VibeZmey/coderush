import { useEffect, useId, useRef, type ReactNode } from 'react';
import { AlertCircle, Check, Copy, LoaderCircle, X } from './icons';

export function Button({ children, busy, variant = 'primary', ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { busy?: boolean; variant?: 'primary' | 'secondary' | 'ghost' }) {
  return <button {...props} disabled={props.disabled || busy} className={`button ${variant} ${props.className || ''}`} aria-busy={busy}>{busy && <span className="busy-bars" aria-hidden="true"><i /><i /><i /></span>}{children}</button>;
}
export function Notice({ children, error = false }: { children: ReactNode; error?: boolean }) {
  return <div className={`notice ${error ? 'error' : ''}`} role={error ? 'alert' : 'status'}><AlertCircle size={18} /><div>{children}</div></div>;
}
export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return <label className="field"><span>{label}</span>{children}{hint && <small>{hint}</small>}</label>;
}
export function Modal({ title, onClose, children, className = '' }: { title: string; onClose?: () => void; children: ReactNode; className?: string }) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  useEffect(() => { const element = dialog.current!; element.showModal(); return () => element.close(); }, []);
  return <dialog ref={dialog} className={`modal ${className}`} onCancel={event => { event.preventDefault(); onClose?.(); }} onClick={e => { if (e.target === e.currentTarget) onClose?.(); }} aria-labelledby={titleId}><div className="modal-content"><div className="section-head"><h2 id={titleId}>{title}</h2>{onClose && <button className="icon-button" onClick={onClose} aria-label="Закрыть"><X size={20} /></button>}</div>{children}</div></dialog>;
}
export function CopyId({ value, onCopied }: { value: string; onCopied: (text: string) => void }) {
  return <button type="button" className="copy-id" title="Скопировать идентификатор" onClick={async () => { try { await navigator.clipboard.writeText(value); onCopied('Идентификатор скопирован'); } catch { onCopied('Не удалось скопировать. Выделите идентификатор вручную.'); } }}><code>{value}</code><Copy size={13} /></button>;
}
export function Empty({ icon, title, children }: { icon: ReactNode; title: string; children: ReactNode }) {
  return <div className="empty"><div className="empty-icon">{icon}</div><h3>{title}</h3><p>{children}</p></div>;
}
export function Success({ children }: { children: ReactNode }) { return <div className="success"><Check size={18} />{children}</div>; }
export function date(value: string) { return new Date(value).toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }); }
