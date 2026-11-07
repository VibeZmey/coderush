import { useRef, useState } from 'react';
import { Download, FileJson, Upload, X } from './icons';
import { api, type ProblemInput } from './api';
import { parseProblemFile } from './problemJson';
import { Button, Modal, Notice, Success } from './components';
import { difficultyNames } from './catalog';

export default function JsonImport({ authorized, notify, onImported, onLoad }: { authorized: () => boolean; notify: (message: string) => void; onImported?: () => void; onLoad?: (problem: ProblemInput) => void }) {
  const [open, setOpen] = useState(false); const [items, setItems] = useState<ProblemInput[]>([]); const [filename, setFilename] = useState('');
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [reading, setReading] = useState(false); const [done, setDone] = useState(0);
  const readSequence = useRef(0);
  async function read(file?: File) {
    if (!file) return;
    const sequence = ++readSequence.current;
    setItems([]); setError(''); setDone(0); setFilename(file.name);
    if (file.size > 8 * 1024 * 1024) { setError('Размер файла не должен превышать 8 МиБ.'); return; }
    setReading(true);
    try { const parsed = parseProblemFile(await file.text()); if (sequence === readSequence.current) setItems(parsed); }
    catch (e) { if (sequence === readSequence.current) setError((e as Error).message); }
    finally { if (sequence === readSequence.current) setReading(false); }
  }
  async function upload() {
    if (!authorized()) return;
    setBusy(true); setError('');
    try { const result = await api.importProblems(items); setDone(result.problems.length); setItems([]); onImported?.(); notify(`Импортировано задач: ${result.problems.length}`); }
    catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  }
  return <><Button type="button" variant="secondary" onClick={() => { if (authorized()) setOpen(true); }}><Upload size={16} />Импорт JSON</Button>{open && <Modal title="Задачи из одного файла" onClose={() => { if (!busy) setOpen(false); }}><p className="muted">Условия, языки, шаблоны и все тесты — в одном JSON. Можно загрузить одну задачу или набор задач. До 50 задач и 8 МиБ.</p><label className={`json-dropzone ${busy || reading ? 'disabled' : ''}`} onDragOver={e => e.preventDefault()} onDrop={e => { e.preventDefault(); if (!busy && !reading) void read(e.dataTransfer.files[0]); }}><FileJson size={30} /><strong>{filename || 'Выбери или перетащи JSON-файл'}</strong><span>{reading ? 'Читаем и проверяем…' : 'JSON-файл с условиями и тестами'}</span><input type="file" accept=".json,application/json" disabled={busy || reading} onChange={e => { void read(e.target.files?.[0]); e.target.value = ''; }} /></label><a className="text-button json-example" href="/problem-example.json" download><Download size={15} />Скачать пример JSON с тестами</a>{error && <Notice error>{error}</Notice>}{done > 0 && <Success>Создано задач: {done}. Условия и тесты сохранены.</Success>}{items.length > 0 && <><div className="import-preview">{items.map(p => <div key={p.key}><FileJson size={17} /><div><strong>{p.title}</strong><span>{p.key} · {difficultyNames[p.difficulty]} · {p.testCases.length} тестов · {p.languages.length} языков</span></div><span className={`badge ${p.publish ? 'green' : ''}`}>{p.publish ? 'Публикация' : 'Черновик'}</span></div>)}</div><Notice>{items.some(p => p.publish) ? 'Задачи, отмеченные для публикации, будут опубликованы сразу.' : 'Задачи будут сохранены как черновики.'}</Notice><div className="inline-actions"><Button busy={busy} disabled={reading} type="button" onClick={upload}><Upload size={16} />Импортировать {items.length === 1 ? 'задачу' : `задачи (${items.length})`}</Button>{onLoad && items.length === 1 && <Button type="button" disabled={busy} variant="secondary" onClick={() => { onLoad(items[0]); setOpen(false); }}>Загрузить в форму</Button>}</div></>}<div className="import-footer"><button type="button" className="text-button" disabled={busy} onClick={() => setOpen(false)}><X size={14} />Закрыть</button></div></Modal>}</>;
}
