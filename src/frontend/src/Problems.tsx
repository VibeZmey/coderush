import { useEffect, useRef, useState } from 'react';
import { ArrowLeft, ArrowRight, Check, ChevronLeft, ChevronRight, Code2, Download, Eye, FileCode2, FlaskConical, Layers3, LoaderCircle, LockKeyhole, Pencil, Plus, RefreshCw, Search, ShieldCheck, X } from './icons';
import ReactMarkdown from 'react-markdown';
import { api, problemToInput, type Account, type ProblemDetails, type ProblemPage, type ProblemSummary } from './api';
import { difficultyNames, languages } from './catalog';
import { Button, CopyId, date, Empty, Field, Notice } from './components';
import Admin from './Admin';
import JsonImport from './JsonImport';

const statusNames = { Draft: 'Черновик', Published: 'Опубликована', Retired: 'Снята с публикации' };
type Shared = { account: Account | null; authorized: () => boolean; notify: (message: string) => void };

export default function Problems(shared: Shared) {
  const [search, setSearch] = useState(''); const [debounced, setDebounced] = useState('');
  const [difficulty, setDifficulty] = useState(''); const [status, setStatus] = useState(''); const [sort, setSort] = useState('newest');
  const [page, setPage] = useState(1); const [pageSize, setPageSize] = useState(20); const [refresh, setRefresh] = useState(0);
  const [data, setData] = useState<ProblemPage | null>(null); const [error, setError] = useState(''); const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<string | null>(null); const [detail, setDetail] = useState<ProblemDetails | null>(null); const [detailError, setDetailError] = useState('');
  const [detailLoading, setDetailLoading] = useState(false); const [editing, setEditing] = useState(false);
  const [detailRefresh, setDetailRefresh] = useState(0);
  const publishingIds = useRef(new Set<string>());
  const [publishing, setPublishing] = useState<string[]>([]);
  const [publishError, setPublishError] = useState('');
  async function publish(problem: ProblemSummary) {
    if (!shared.authorized() || publishingIds.current.has(problem.id)) return;
    publishingIds.current.add(problem.id);
    setPublishing(ids => [...ids, problem.id]);
    setPublishError('');
    try {
      await api.publishProblem(problem.id, problem.versionId);
      shared.notify(`Задача «${problem.title}» опубликована`);
      setRefresh(n => n + 1);
    } catch (e) { setPublishError((e as Error).message); }
    finally {
      publishingIds.current.delete(problem.id);
      setPublishing(ids => ids.filter(id => id !== problem.id));
    }
  }

  useEffect(() => { const timer = setTimeout(() => setDebounced(search), 300); return () => clearTimeout(timer); }, [search]);
  useEffect(() => {
    if (!shared.account) { setData(null); return; }
    let active = true;
    setLoading(true); setError('');
    const params = new URLSearchParams({ search: debounced, sort, page: String(page), pageSize: String(pageSize) });
    if (difficulty) params.set('difficulty', difficulty);
    if (status) params.set('status', status);
    api.problems(params).then(result => { if (active) { setData(result); if (page > 1 && result.total <= (page - 1) * pageSize) setPage(Math.max(1, Math.ceil(result.total / pageSize))); } })
      .catch(e => { if (active) { setError(e.message); setData(null); } }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [shared.account?.id, debounced, difficulty, status, sort, page, pageSize, refresh]);
  useEffect(() => {
    if (!selected || !shared.account) return;
    let active = true;
    setDetailLoading(true); setDetailError(''); setDetail(null);
    api.problem(selected).then(result => { if (active) setDetail(result); }).catch(e => { if (active) setDetailError(e.message); }).finally(() => { if (active) setDetailLoading(false); });
    return () => { active = false; };
  }, [selected, detailRefresh, shared.account?.id]);
  function reset() { setSearch(''); setDebounced(''); setDifficulty(''); setStatus(''); setSort('newest'); setPage(1); }
  function open(id: string) { setSelected(id); setEditing(false); window.scrollTo(0, 0); }
  function back() { setSelected(null); setDetail(null); setEditing(false); window.scrollTo(0, 0); }
  if (selected && detail && editing) return <Admin {...shared} key={detail.summary.versionId} initialProblem={problemToInput(detail)} editing={{ id: selected, versionId: detail.summary.versionId, versionNumber: detail.summary.versionNumber }} onCancel={() => setEditing(false)} onSaved={() => { setEditing(false); setDetailRefresh(n => n + 1); setRefresh(n => n + 1); window.scrollTo(0, 0); }} />;
  if (selected) return <><div className="problem-back"><Button variant="ghost" onClick={back}><ArrowLeft size={16} />Все задачи</Button></div>{detailLoading ? <Loading /> : detailError ? <div className="panel"><Notice error>{detailError}</Notice><Button variant="secondary" onClick={() => setDetailRefresh(n => n + 1)}><RefreshCw size={16} />Повторить</Button></div> : detail && <ProblemView key={detail.summary.versionId} problem={detail} notify={shared.notify} onEdit={() => { setEditing(true); window.scrollTo(0, 0); }} />}</>;
  return <>
    <div className="page-heading"><div><span className="eyebrow">БИБЛИОТЕКА · ADMIN</span><h1>Задачи под контролем.</h1><p>Находи, изучай и совершенствуй задачи вместе со всеми тестами.</p></div><div className="inline-actions"><JsonImport {...shared} onImported={() => setRefresh(n => n + 1)} /><Button onClick={() => { location.hash = 'admin'; }}><Plus size={16} />Создать задачу</Button></div></div>
    
    {!shared.account ? <section className="panel"><Empty icon={<LockKeyhole size={28} />} title="Войди в аккаунт администратора">Каталог содержит полные условия и скрытые тесты. Для просмотра необходима роль Admin.</Empty><div className="center-action"><Button onClick={shared.authorized}>Войти<ArrowRight size={16} /></Button></div></section> : <>
      <section className="panel catalog-panel"><div className="catalog-toolbar"><label className="catalog-search"><Search size={18} /><input type="search" aria-label="Поиск задач" placeholder="Найти по названию или ключу…" value={search} maxLength={200} onChange={e => { setSearch(e.target.value); setPage(1); }} />{search && <button className="icon-button" aria-label="Очистить поиск" onClick={() => { setSearch(''); setPage(1); }}><X size={14} /></button>}</label><Button variant="secondary" busy={loading} onClick={() => setRefresh(n => n + 1)} aria-label="Обновить каталог"><RefreshCw size={16} /></Button></div><div className="catalog-filters"><Field label="Сложность"><select value={difficulty} onChange={e => { setDifficulty(e.target.value); setPage(1); }}><option value="">Любая сложность</option>{Object.entries(difficultyNames).map(([key, value]) => <option key={key} value={key}>{value}</option>)}</select></Field><Field label="Публикация"><select value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}><option value="">Все статусы</option>{Object.entries(statusNames).map(([key, value]) => <option key={key} value={key}>{value}</option>)}</select></Field><Field label="Сортировка"><select value={sort} onChange={e => { setSort(e.target.value); setPage(1); }}><option value="newest">Сначала новые версии</option><option value="oldest">Сначала старые версии</option><option value="title">Название: А → Я</option><option value="title-desc">Название: Я → А</option><option value="difficulty">От простого к сложному</option><option value="tests">Больше тестов</option></select></Field><button className="text-button filter-reset" onClick={reset}>Сбросить</button></div>
        <div className="catalog-results-heading"><span>{loading ? 'Загружаем задачи…' : `Найдено задач: ${data?.total ?? 0}`}</span><span>Последняя версия каждой задачи</span></div>
        {publishError && <Notice error>{publishError}</Notice>}<div aria-busy={loading}>{loading ? <Loading /> : error ? <Notice error>{error}</Notice> : data?.items.length ? <div className="table-scroll"><table className="problem-table"><thead><tr><th>Задача</th><th>Сложность</th><th>Статус</th><th>Тесты / языки</th><th>Версия</th><th><span className="sr-only">Публикация и просмотр</span></th></tr></thead><tbody>{data.items.map(p => <tr key={p.id}><td><button className="problem-title-button" onClick={() => open(p.id)}>{p.title}</button><code className="problem-key">{p.key}</code></td><td><span className={`badge difficulty-${p.difficulty}`}>{difficultyNames[p.difficulty]}</span></td><td><span className={`badge ${p.status === 'Published' ? 'green' : ''}`}>{statusNames[p.status]}</span></td><td><span className="table-count"><FlaskConical size={14} />{p.testCount}<span> / </span><Code2 size={14} />{p.languageCount}</span></td><td><span className="version-label">v{p.versionNumber}</span><small>{date(p.createdAt)}</small></td><td><div className="problem-row-actions"><button type="button" className={`icon-button publish-button ${p.status === 'Published' ? 'published' : ''}`} aria-label={p.status === 'Published' ? `Задача ${p.title} опубликована` : `Опубликовать задачу ${p.title}`} title={p.status === 'Published' ? 'Опубликована' : 'Опубликовать'} disabled={p.status === 'Published' || publishing.includes(p.id)} aria-busy={publishing.includes(p.id)} onClick={() => publish(p)}>{publishing.includes(p.id) ? <LoaderCircle size={17} className="spin" /> : <Check size={17} />}</button><button className="icon-button" aria-label={`Открыть задачу ${p.title}`} onClick={() => open(p.id)}><ArrowRight size={17} /></button></div></td></tr>)}</tbody></table></div> : <Empty icon={<Search size={27} />} title={search || difficulty || status ? 'Ничего не найдено' : 'Коллекция начинается с первой задачи'}>{search || difficulty || status ? 'Попробуй изменить запрос или сбросить фильтры.' : 'Создай задачу вручную или импортируй JSON с условиями и тестами.'}</Empty>}</div>
        <div className="catalog-pagination"><label>На странице<select aria-label="Задач на странице" value={pageSize} onChange={e => { setPageSize(Number(e.target.value)); setPage(1); }}>{[10, 20, 50, 100].map(n => <option value={n} key={n}>{n}</option>)}</select></label><div><button className="icon-button" disabled={loading || page <= 1} aria-label="Предыдущая страница" onClick={() => setPage(p => p - 1)}><ChevronLeft size={18} /></button><span>{page} / {Math.max(1, Math.ceil((data?.total || 0) / pageSize))}</span><button className="icon-button" disabled={loading || !data || page * pageSize >= data.total} aria-label="Следующая страница" onClick={() => setPage(p => p + 1)}><ChevronRight size={18} /></button></div></div>
      </section>
    </>}
  </>;
}

function Loading() { return <div className="catalog-loading" role="status"><div className="catalog-skeleton" aria-hidden="true"><i className="skeleton" /><i className="skeleton" /><i className="skeleton" /></div><span>Загружаем из каталога…</span></div>; }

function Markdown({ value }: { value: string }) {
  return <div className="problem-markdown"><ReactMarkdown disallowedElements={['img']} components={{ a: ({ children, href }) => <a href={href} target="_blank" rel="noopener noreferrer">{children}</a> }}>{value}</ReactMarkdown></div>;
}

function ProblemView({ problem, notify, onEdit }: { problem: ProblemDetails; notify: (message: string) => void; onEdit: () => void }) {
  const [tab, setTab] = useState<'statement' | 'tests' | 'languages'>('statement'); const [testFilter, setTestFilter] = useState('all'); const [testSearch, setTestSearch] = useState(''); const [testPage, setTestPage] = useState(1);
  const { summary: p } = problem;
  const tests = problem.testCases.map((t, i) => ({ ...t, position: i + 1 })).filter(t => (testFilter === 'all' || (testFilter === 'sample' ? t.isSample : !t.isSample)) && (!testSearch || t.input.toLowerCase().includes(testSearch.toLowerCase()) || t.expectedOutput.toLowerCase().includes(testSearch.toLowerCase())));
  function download() { const url = URL.createObjectURL(new Blob([JSON.stringify(problemToInput(problem), null, 2)], { type: 'application/json;charset=utf-8' })); const a = document.createElement('a'); a.href = url; a.download = `${p.key}.json`; a.click(); setTimeout(() => URL.revokeObjectURL(url), 1000); }
  return <><div className="page-heading"><div><span className="eyebrow">ЗАДАЧА · ВЕРСИЯ {p.versionNumber}</span><h1>{p.title}</h1><p className="mono">{p.key}</p></div><div className="inline-actions"><Button variant="secondary" onClick={download}><Download size={16} />JSON с тестами</Button><Button onClick={onEdit}><Pencil size={16} />Редактировать</Button></div></div><div className="problem-meta"><span className={`badge difficulty-${p.difficulty}`}>{difficultyNames[p.difficulty]}</span><span className={`badge ${p.status === 'Published' ? 'green' : ''}`}>{statusNames[p.status]}</span><span><FlaskConical size={15} />{problem.testCases.length} тестов</span><span><Code2 size={15} />{problem.languages.length} языков</span><span>{date(p.createdAt)}</span></div>
    <div className="problem-detail-layout"><section className="panel problem-detail-main"><div className="admin-tabs" role="tablist" aria-label="Содержимое задачи">{([{ id: 'statement', label: 'Условие', icon: FileCode2 }, { id: 'tests', label: `Тесты (${problem.testCases.length})`, icon: FlaskConical }, { id: 'languages', label: 'Языки и шаблоны', icon: Code2 }] as const).map(t => <button role="tab" id={`view-tab-${t.id}`} aria-controls="problem-view-panel" aria-selected={tab === t.id} key={t.id} className={tab === t.id ? 'selected' : ''} onClick={() => setTab(t.id)}><t.icon size={15} />{t.label}</button>)}</div><div className="problem-detail-body" role="tabpanel" id="problem-view-panel" aria-labelledby={`view-tab-${tab}`}>
      {tab === 'statement' && <><Markdown value={problem.descriptionMarkdown} />{([{ key: 'inputDescriptionMarkdown', title: 'Входные данные' }, { key: 'outputDescriptionMarkdown', title: 'Выходные данные' }, { key: 'constraintsMarkdown', title: 'Ограничения' }, { key: 'explanationMarkdown', title: 'Пояснение' }] as const).map(f => problem[f.key] && <section className="statement-section" key={f.key}><h2>{f.title}</h2><Markdown value={problem[f.key]} /></section>)}<div className="inline-actions statement-actions"><Button variant="secondary" onClick={() => setTab('tests')}><Eye size={16} />Посмотреть все тесты</Button></div></>}
      {tab === 'tests' && <><div className="test-view-toolbar"><label className="catalog-search"><Search size={16} /><input aria-label="Поиск по тестам" placeholder="Поиск по вводу и ожидаемому выводу" value={testSearch} onChange={e => { setTestSearch(e.target.value); setTestPage(1); }} /></label><select aria-label="Тип тестов" value={testFilter} onChange={e => { setTestFilter(e.target.value); setTestPage(1); }}><option value="all">Все тесты</option><option value="sample">Открытые примеры</option><option value="hidden">Скрытые тесты</option></select></div><p className="muted small test-count">Найдено тестов: {tests.length}</p><div className="test-case-list">{tests.slice((testPage - 1) * 20, testPage * 20).map(t => <article className="nested-card" key={t.position}><div className="section-head"><strong>Тест #{t.position}</strong><span className={`badge ${t.isSample ? 'green' : ''}`}>{t.isSample ? <Eye size={12} /> : <LockKeyhole size={12} />}{t.isSample ? 'Пример в условии' : 'Скрытый тест'}</span></div><div className="test-io-grid"><div><span>Входные данные</span><pre>{t.input || <em>Пустой ввод</em>}</pre></div><div><span>Ожидаемый вывод</span><pre>{t.expectedOutput || <em>Пустой вывод</em>}</pre></div></div></article>)}</div>{!tests.length && <Empty icon={<Search size={25} />} title="Тесты не найдены">Измени поисковый запрос или тип тестов.</Empty>}{tests.length > 20 && <div className="test-pagination"><Button variant="secondary" disabled={testPage === 1} onClick={() => setTestPage(n => n - 1)}><ChevronLeft size={15} /></Button><span>{testPage} / {Math.ceil(tests.length / 20)}</span><Button variant="secondary" disabled={testPage * 20 >= tests.length} onClick={() => setTestPage(n => n + 1)}><ChevronRight size={15} /></Button></div>}<div className="statement-actions"><Button variant="secondary" onClick={onEdit}><Pencil size={16} />Изменить, добавить или удалить тесты</Button></div></>}
      {tab === 'languages' && <div className="test-case-list">{problem.languages.map(l => <article className="nested-card" key={l.programmingLanguageId}><div className="section-head"><strong>{languages.find(lang => lang.id === l.programmingLanguageId)?.name || `Среда #${l.programmingLanguageId}`}</strong><span className="badge">#{l.programmingLanguageId}</span></div><pre className="template-preview">{l.template || <em>Пустой шаблон</em>}</pre></article>)}</div>}
    </div></section><aside className="problem-detail-side"><section className="panel"><h2>Лимиты выполнения</h2><dl className="problem-limits"><dt>CPU</dt><dd>{problem.cpuTimeLimitSeconds} сек.</dd><dt>Общее время</dt><dd>{problem.wallTimeLimitSeconds} сек.</dd><dt>Память</dt><dd>{problem.memoryLimitKilobytes.toLocaleString('ru-RU')} КиБ</dd><dt>Размер вывода</dt><dd>{problem.outputLimitKilobytes.toLocaleString('ru-RU')} КиБ</dd></dl><div className="problem-identifiers"><Field label="ID задачи"><CopyId value={p.id} onCopied={notify} /></Field><Field label="ID версии"><CopyId value={p.versionId} onCopied={notify} /></Field></div></section></aside></div>
  </>;
}
