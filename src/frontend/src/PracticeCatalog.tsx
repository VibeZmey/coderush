import { useEffect, useRef, useState } from 'react';
import { api, type Account, type Difficulty, type PracticeProblem } from './api';
import { difficultyNames } from './catalog';
import { Button, Empty, Modal, Notice } from './components';
import { ArrowRight, Check, Code2, Search } from './icons';
import './practice.css';

const levels: Difficulty[] = ['Easy', 'Medium', 'Hard'];
const levelDescriptions = {
  Easy: 'Разомнитесь на базовых задачах.',
  Medium: 'Подберите подходящий алгоритм.',
  Hard: 'Разберите нетривиальный случай.',
};
const pageSize = 20;
const fold = (value: string) => value.toLocaleLowerCase('ru');

export default function PracticeCatalog({ account }: { account: Account | null }) {
  const [items, setItems] = useState<PracticeProblem[] | null>(null);
  const [error, setError] = useState('');
  const [refresh, setRefresh] = useState(0);
  useEffect(() => {
    let active = true;
    void api.practiceCatalog().then(data => { if (active) setItems(data); })
      .catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [account?.id, refresh]);

  return <div className="practice-catalog practice-enter">
    <div className="page-heading"><div><span className="eyebrow">САМОСТОЯТЕЛЬНАЯ ПРАКТИКА</span><h1>Задачи</h1><p>Выберите интересную задачу и решайте в своём темпе, без таймера.</p></div></div>
    {!items && !error ? <div aria-busy="true" aria-label="Загружаем каталог" className="practice-skeleton">{levels.map(level => <div key={level} className="skeleton" />)}</div>
      : error ? <section className="panel"><Notice error>{error}</Notice><Button variant="secondary" onClick={() => { setError(''); setRefresh(n => n + 1); }}>Повторить</Button></section>
      : !items?.length ? <section className="panel"><Empty icon={<Code2 size={28} />} title="В каталоге пока нет задач">Здесь появятся опубликованные задачи для самостоятельной практики.</Empty></section>
      : <PracticeList items={items} signedIn={!!account} />}
  </div>;
}

function PracticeList({ items, signedIn, currentId, onSelect, busy }: {
  items: PracticeProblem[]; signedIn: boolean; currentId?: string; onSelect?: (id: string) => void; busy?: boolean;
}) {
  const [search, setSearch] = useState('');
  const [topic, setTopic] = useState('');
  const [difficulty, setDifficulty] = useState<Difficulty | ''>('');
  const [unsolved, setUnsolved] = useState(false);
  const [visible, setVisible] = useState(pageSize);
  const sentinel = useRef<HTMLDivElement>(null);
  const drawer = !!onSelect;
  const topicCounts = new Map<string, { name: string; count: number }>();
  for (const item of items) {
    for (const name of item.topics) {
      const key = fold(name);
      const entry = topicCounts.get(key);
      if (entry) entry.count++;
      else topicCounts.set(key, { name, count: 1 });
    }
  }
  const topics = [...topicCounts].sort(([, a], [, b]) => b.count - a.count || a.name.localeCompare(b.name, 'ru'));
  const query = fold(search.trim());
  const results = items.filter(item => (!difficulty || item.difficulty === difficulty) &&
    (!topic || item.topics.some(name => fold(name) === topic)) && (!unsolved || !item.solved) &&
    (!query || fold(item.title).includes(query) || item.key.includes(query)));
  const hasMore = visible < results.length;
  useEffect(() => {
    const target = sentinel.current;
    if (!hasMore || !target || busy) return;
    const observer = new IntersectionObserver(entries => {
      if (!entries.some(entry => entry.isIntersecting)) return;
      observer.disconnect();
      setVisible(visible + pageSize);
    }, { root: target.closest('dialog') });
    observer.observe(target);
    return () => observer.disconnect();
  }, [hasMore, visible, search, topic, difficulty, unsolved, busy]);

  function reset() {
    setSearch(''); setDifficulty(''); setTopic(''); setUnsolved(false); setVisible(pageSize);
  }
  return <section aria-label="Каталог задач" className="practice-list">
    {!drawer && <div aria-label="Сложность" className="practice-levels">{levels.map(level =>
      <button className="practice-level" key={level} aria-pressed={difficulty === level}
        onClick={() => { setDifficulty(current => current === level ? '' : level); setVisible(pageSize); }}>
        <span className={`badge difficulty-${level}`}>{difficultyNames[level]}</span>
        <span className="practice-level-copy">{levelDescriptions[level]}</span>
        <span className="muted">{items.filter(item => item.difficulty === level).length} в каталоге</span>
      </button>)}</div>}
    {!drawer && <div className="practice-topics" aria-label="Темы">
      <button aria-pressed={!topic} onClick={() => { setTopic(''); setVisible(pageSize); }}>Все темы</button>
      {topics.map(([key, { name, count }]) => <button key={key} aria-pressed={topic === key}
        onClick={() => { setTopic(current => current === key ? '' : key); setVisible(pageSize); }}>{name} <span>{count}</span></button>)}
    </div>}
    <div className="practice-toolbar">
      <label className="catalog-search"><Search size={18} /><input type="search" aria-label="Поиск задач" placeholder="Найти задачу…"
        value={search} onChange={e => { setSearch(e.target.value); setVisible(pageSize); }} /></label>
      {drawer && <>
        <select aria-label="Сложность" value={difficulty} onChange={e => { setDifficulty(e.target.value as Difficulty | ''); setVisible(pageSize); }}>
          <option value="">Все сложности</option>{levels.map(level => <option key={level} value={level}>{difficultyNames[level]}</option>)}
        </select>
        <select aria-label="Тема" value={topic} onChange={e => { setTopic(e.target.value); setVisible(pageSize); }}>
          <option value="">Все темы</option>{topics.map(([key, { name }]) => <option key={key} value={key}>{name}</option>)}
        </select>
      </>}
      {signedIn && <label className="checkbox"><input type="checkbox" checked={unsolved}
        onChange={e => { setUnsolved(e.target.checked); setVisible(pageSize); }} />Только нерешённые</label>}
      {!drawer && signedIn && <span className="practice-progress"><Check size={16} />Решено {items.filter(item => item.solved).length} из {items.length}</span>}
    </div>
    {results.length ? <PracticeRows items={results.slice(0, visible)} currentId={currentId} onSelect={onSelect} busy={busy} />
      : <div className="practice-no-results"><p className="muted">Задачи не найдены.</p><Button variant="secondary" onClick={reset}>Сбросить фильтры</Button></div>}
    {hasMore && <div className="practice-more" ref={sentinel}><Button variant="secondary" disabled={busy}
      onClick={() => setVisible(visible + pageSize)}>Показать ещё задачи</Button></div>}
  </section>;
}

export function practiceHref(id: string) {
  return `#practice?id=${encodeURIComponent(id)}`;
}

function PracticeRows({ items, currentId, onSelect, busy }: {
  items: PracticeProblem[]; currentId?: string; onSelect?: (id: string) => void; busy?: boolean;
}) {
  return <ul className="practice-rows">{items.map(item => {
    const content = <>
      <span className={item.solved ? 'practice-solved' : 'muted'} title={item.solved ? 'Решена' : undefined}>
        {item.solved ? <><Check size={18} /><span className="sr-only">Решена</span></> : <Code2 size={18} />}
      </span>
      <span className="practice-row-title"><strong>{item.title}</strong><small>{item.topics.join(' · ')}</small></span>
      <span className={`badge difficulty-${item.difficulty}`}>{difficultyNames[item.difficulty]}</span>
      <ArrowRight size={16} className="practice-row-arrow" />
    </>;
    return <li key={item.id}>{onSelect
      ? <button type="button" aria-current={item.id === currentId ? 'page' : undefined} disabled={busy}
          onClick={() => onSelect(item.id)}>{content}</button>
      : <a href={practiceHref(item.id)}>{content}</a>}</li>;
  })}</ul>;
}

export function PracticeDrawer({ items, currentId, onSelect, onClose, loading, error, retry, busy, signedIn }: {
  items: PracticeProblem[]; currentId: string; onSelect: (id: string) => void; onClose: () => void;
  loading: boolean; error: string; retry: () => void; busy: boolean; signedIn: boolean;
}) {
  const [closing, setClosing] = useState(false);
  const closeAction = useRef<(() => void) | null>(null);
  function close(action: () => void) {
    if (closeAction.current) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) { action(); return; }
    closeAction.current = action;
    setClosing(true);
  }
  return <Modal title="Список задач" className={`practice-drawer${closing ? ' practice-drawer-closing' : ''}`}
    onClose={() => close(onClose)} onAnimationEnd={() => { if (closing) closeAction.current?.(); }}>
    {error ? <><Notice error>{error}</Notice><Button variant="secondary" onClick={retry}>Повторить</Button></>
      : loading ? <p role="status">Загружаем задачи…</p>
      : <PracticeList items={items} signedIn={signedIn} currentId={currentId} busy={busy || closing}
          onSelect={id => close(() => onSelect(id))} />}
  </Modal>;
}
