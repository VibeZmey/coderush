import { useEffect, useState, type FormEvent } from 'react';
import { ArrowDownToLine, ArrowRight, ArrowUpRight, Braces, Check, ChevronRight, CircleHelp, Clock3, Code2, Flame, Home, Layers3, LogOut, Menu, Moon, Play, Plus, Settings2, ShieldCheck, Sparkles, Sun, Target, Terminal, Trophy, User, X, Zap } from './icons';
import { api, ApiError, type Account, type Difficulty, type MatchResult, type RunResult } from './api';
import { difficultyNames } from './catalog';
import { Button, CopyId, date, Empty, Field, Modal, Notice, Success } from './components';
import Admin from './Admin';
import TrainingWorkspace from './TrainingWorkspace';
import Problems from './Problems';
import Arena from './Arena';
import Duels, { DuelOpponentProfile } from './Duels';
import { ProfileDuelRating } from './DuelRating';


type Page = 'home' | 'training' | 'duels' | 'workspace' | 'problems' | 'admin' | 'profile' | 'opponent';
type LocalMatch = MatchResult & { count?: number; difficulty?: Difficulty; seconds?: number };


const pages: { id: Page; name: string; icon: typeof Home }[] = [
  { id: 'home', name: 'Обзор', icon: Home }, { id: 'training', name: 'Тренировки', icon: Target },
  { id: 'duels', name: 'Дуэли', icon: Trophy },
  { id: 'problems', name: 'Задачи', icon: Layers3 },
];
function getPage(): Page { const hash = location.hash.slice(1).split('?')[0]; if (hash === 'workspace' && !new URLSearchParams(location.hash.split('?')[1] || '').get('match')) return 'training'; return [...pages.map(p => p.id), 'admin', 'profile', 'workspace', 'opponent'].includes(hash) ? hash as Page : 'home'; }
function readTheme(): 'dark' | 'light' { try { return localStorage.getItem('coderush-theme') === 'light' ? 'light' : 'dark'; } catch { return 'dark'; } }

export default function App() {
  const [page, setPage] = useState<Page>(getPage);
  const [matchId, setMatchId] = useState(() => new URLSearchParams(location.hash.split('?')[1] || '').get('match') || '');
  const [theme, setTheme] = useState(readTheme);
  const [account, setAccount] = useState<Account | null>(null);
  const [checking, setChecking] = useState(true);
  const [auth, setAuth] = useState<'login' | 'register' | null>(null);
  const [mobile, setMobile] = useState(false);
  const [toast, setToast] = useState('');
  const [matches, setMatches] = useState<LocalMatch[]>([]);
  const [runs, setRuns] = useState<RunResult[]>([]);

  const [loggingOut, setLoggingOut] = useState(false);
  const [help, setHelp] = useState(false);
  const [sessionError, setSessionError] = useState('');
  useEffect(() => {
    const expired = () => { setAccount(null); setMatches([]); setRuns([]); };
    window.addEventListener('session-expired', expired);
    let active = true;
    api.me().then(a => { if (active) setAccount(a); }).catch(e => { if (active && !(e instanceof ApiError && e.status === 401)) setSessionError(e.message); }).finally(() => { if (active) setChecking(false); });
    return () => { active = false; window.removeEventListener('session-expired', expired); };
  }, []);
  useEffect(() => { const change = () => { setPage(getPage()); setMatchId(new URLSearchParams(location.hash.split('?')[1] || '').get('match') || ''); setMobile(false); window.scrollTo(0, 0); }; window.addEventListener('hashchange', change); return () => window.removeEventListener('hashchange', change); }, []);
  useEffect(() => { document.documentElement.dataset.theme = theme; try { localStorage.setItem('coderush-theme', theme); } catch { /* Theme still works without storage. */ } }, [theme]);
  useEffect(() => { if (!mobile) return; const close = (e: KeyboardEvent) => { if (e.key === 'Escape') setMobile(false); }; document.body.style.overflow = 'hidden'; window.addEventListener('keydown', close); return () => { document.body.style.overflow = ''; window.removeEventListener('keydown', close); }; }, [mobile]);
  useEffect(() => { if (!toast) return; const timer = setTimeout(() => setToast(''), 4200); return () => clearTimeout(timer); }, [toast]);
  useEffect(() => { document.title = `${pages.find(p => p.id === page)?.name || (page === 'admin' ? 'Создать задачу' : page === 'workspace' ? 'Тренировка' : 'Профиль')} · CodeRush`; }, [page]);
  function navigate(to: Page) { location.hash = to === 'workspace' && matchId ? `workspace?match=${matchId}` : to; setMobile(false); }
  function openMatch(id: string) { setMatchId(id); location.hash = 'workspace?match=' + encodeURIComponent(id); }
  async function logout() {
    setLoggingOut(true);
    try { await api.logout(); setAccount(null); setMatches([]); setRuns([]); navigate('home'); setToast('Вы вышли из аккаунта'); }
    catch (e) { setToast((e as Error).message); } finally { setLoggingOut(false); }
  }
  function authorized() { if (account) return true; setAuth('login'); return false; }
  const shared = { account, authorized, notify: setToast };
  return <>
    <a className="skip-link" href="#main" onClick={e => { e.preventDefault(); document.getElementById('main')?.focus(); }}>Перейти к содержимому</a>
    <header className="arena-nav">
      <a className="arena-brand" href="#home" aria-label="CodeRush, главная"><img src="/logo.svg" alt="" /><span>code<span>rush</span><i>_</i></span></a>
      <nav id="primary-nav" className={mobile ? 'arena-nav-links open' : 'arena-nav-links'} aria-label="Основная навигация" onClick={event => { if ((event.target as HTMLElement).closest('a')) setMobile(false); }}>{pages.map(({ id, name }) => <a key={id} href={`#${id}`} className={page === id ? 'active' : ''} aria-current={page === id ? 'page' : undefined}>{id === 'home' ? 'Главная' : name}</a>)}<a href="#admin" className={page === 'admin' ? 'active' : ''} aria-current={page === 'admin' ? 'page' : undefined}>Создать задачу</a></nav>
      <div className="arena-nav-actions"><button className="icon-button" onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')} aria-label={theme === 'dark' ? 'Включить светлую тему' : 'Включить тёмную тему'}>{theme === 'dark' ? <Sun size={20} /> : <Moon size={20} />}</button>{checking ? <span className="skeleton nav-skeleton" aria-label="Проверяем вход" /> : account ? <button className="avatar" onClick={() => navigate('profile')} aria-label="Открыть профиль">{(account.userName || 'U').slice(0, 2).toUpperCase()}</button> : <Button variant="secondary" onClick={() => setAuth('login')}>Войти <ArrowUpRight size={16} /></Button>}<button className={`menu-toggle ${mobile ? 'is-open' : ''}`} onClick={() => setMobile(!mobile)} aria-expanded={mobile} aria-controls="primary-nav" aria-label={mobile ? 'Закрыть меню' : 'Открыть меню'}><span /><span /></button></div>
    </header>
    <div className="app-shell">
      <main id="main" key={page} tabIndex={-1}>
        {sessionError && <Notice error>{sessionError} <button className="text-button" onClick={() => location.reload()}>Переподключиться</button></Notice>}
        {page === 'home' && <Arena name={account?.userName ?? undefined} matches={matches.length} runs={runs.length} start={() => navigate('training')} />}
        {page === 'training' && <Training {...shared} key={`training-${account?.id || 'guest'}`} matches={matches} onMatch={match => setMatches(current => [match, ...current.filter(m => m.id !== match.id)].slice(0, 100))} openMatch={openMatch} />}
        {page === 'duels' && <Duels key={account?.id || 'guest'} account={account} authorized={authorized} theme={theme} />}
        {page === 'workspace' && <TrainingWorkspace {...shared} key={`workspace-${account?.id || 'guest'}-${matchId}`} matchId={matchId} theme={theme} onRun={run => setRuns(current => [run, ...current].slice(0, 100))} />}
        {page === 'problems' && <Problems {...shared} key={`problems-${account?.id || 'guest'}`} />}
        {page === 'admin' && <Admin {...shared} />}
        {page === 'opponent' && (matchId ? <DuelOpponentProfile key={matchId} matchId={matchId} /> : <a href="#duels">К дуэлям</a>)}
        {page === 'profile' && <><PageTitle eyebrow="АККАУНТ" title="Твой профиль." description="Профиль и настройки, с которыми приятно возвращаться к коду." />{account && <ProfileDuelRating key={account.id} accountId={account.id} />}<div className="profile-grid"><section className="panel profile-card"><div className="profile-avatar"><User size={32} /></div><h2>{account?.userName || 'Давай познакомимся'}</h2><p>{account?.email || 'Войди, чтобы создавать тренировки и запускать код.'}</p>{account ? <><Field label="ID аккаунта"><CopyId value={account.id} onCopied={setToast} /></Field><Button variant="secondary" busy={loggingOut} onClick={logout}><LogOut size={16} />Выйти из аккаунта</Button></> : <Button onClick={() => setAuth('login')}>Войти в CodeRush<ArrowRight size={17} /></Button>}</section><section className="panel settings-card"><h2>Внешний вид</h2><p className="muted">Выбери настроение своего рабочего пространства.</p><div className="theme-options">{(['dark', 'light'] as const).map(t => <button key={t} className={`theme-option ${theme === t ? 'selected' : ''}`} onClick={() => setTheme(t)} aria-pressed={theme === t}><div className={`theme-preview ${t}`}><i /><i /><i /></div><span>{t === 'dark' ? <Moon size={16} /> : <Sun size={16} />}{t === 'dark' ? 'Тёмная' : 'Светлая'}{theme === t && <Check size={16} />}</span></button>)}</div></section></div></>}
      </main><footer className="main-footer"><a className="arena-brand" href="#home"><img src="/logo.svg" alt="" /><span>coderush_</span></a><span>Пиши код. Держи темп.</span><button className="text-button" onClick={() => setHelp(true)}>Как это работает <ArrowUpRight size={16} /></button></footer>
    </div>
    {auth && <AuthModal mode={auth} setMode={setAuth} onClose={() => setAuth(null)} onSuccess={a => { setAccount(a); setSessionError(''); setAuth(null); setToast(`С возвращением, ${a.userName || 'разработчик'}`); }} />}
    {help && <Modal title="От первой строки до уверенного решения" onClose={() => setHelp(false)}><div className="help-steps"><p><strong>01 / Создай аккаунт</strong><br />Он понадобится для тренировок и отправки кода.</p><p><strong>02 / Настрой тренировку</strong><br />Выбери сложность, количество задач и время. Нажми «Начать тренировку», чтобы перейти к задачам.</p><p><strong>03 / Пиши код</strong><br />Редактор поддерживает языки из каталога проекта. Условие, языки и стартовый код выдаются автоматически.</p></div></Modal>}
    {toast && <div className="toast" role="status"><Check size={18} /><span>{toast}</span><button className="icon-button" aria-label="Скрыть уведомление" onClick={() => setToast('')}><X size={15} /></button></div>}
  </>;
}

function PageTitle({ eyebrow, title, description, action }: { eyebrow: string; title: string; description: string; action?: React.ReactNode }) {
  return <div className="page-heading"><div><span className="eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>{action}</div>;
}

function AuthModal({ mode, setMode, onClose, onSuccess }: { mode: 'login' | 'register'; setMode: (mode: 'login' | 'register') => void; onClose: () => void; onSuccess: (account: Account) => void }) {
  const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [registered, setRegistered] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget); setBusy(true); setError('');
    const email = String(data.get('email')); const password = String(data.get('password'));
    try {
      if (mode === 'register') { await api.register(email, password, String(data.get('userName'))); setRegistered(true); setMode('login'); }
      else { await api.login(email, password, data.get('remember') === 'on'); onSuccess(await api.me()); }
    } catch (e) { setError(e instanceof ApiError && e.status === 401 ? 'Неверная почта или пароль. После нескольких попыток вход временно блокируется.' : (e as Error).message); } finally { setBusy(false); }
  }
  return <Modal title={mode === 'login' ? 'С возвращением.' : 'Начни свою историю.'} onClose={onClose}><p className="muted">{mode === 'login' ? 'Твоё пространство для кода уже ждёт.' : 'Одна тренировка ближе к следующему уровню.'}</p>{registered && <Success>Аккаунт создан. Теперь войди с почтой и паролем.</Success>}<form className="form-stack" onSubmit={submit} key={mode}>{mode === 'register' && <Field label="Имя пользователя"><input name="userName" required maxLength={256} autoComplete="username" placeholder="Например, code_ninja" /></Field>}<Field label="Электронная почта"><input type="email" name="email" required maxLength={256} autoComplete={mode === 'login' ? 'username' : 'email'} placeholder="you@example.com" /></Field><Field label="Пароль" hint={mode === 'register' ? 'От 10 символов: заглавная и строчная латинские буквы, цифра и специальный символ.' : undefined}><input type="password" name="password" required minLength={mode === 'register' ? 10 : 1} maxLength={1024} autoComplete={mode === 'login' ? 'current-password' : 'new-password'} /></Field>{mode === 'login' && <label className="checkbox"><input type="checkbox" name="remember" />Запомнить меня на этом устройстве</label>}{error && <Notice error>{error}</Notice>}<Button type="submit" busy={busy}>{mode === 'login' ? 'Войти' : 'Создать аккаунт'}<ArrowRight size={17} /></Button></form><p className="auth-switch">{mode === 'login' ? 'Пока нет аккаунта?' : 'Уже есть аккаунт?'} <button className="text-button" onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setError(''); setRegistered(false); }}>{mode === 'login' ? 'Присоединиться' : 'Войти'}</button></p></Modal>;
}

type Shared = { account: Account | null; authorized: () => boolean; notify: (text: string) => void };
function Training({ matches, onMatch, openMatch, ...shared }: Shared & { matches: LocalMatch[]; onMatch: (match: LocalMatch) => void; openMatch: (id: string) => void }) {
  const [difficulty, setDifficulty] = useState<Difficulty>('Easy');
  const [count, setCount] = useState(2);
  const [seconds, setSeconds] = useState(300);
  const [busy, setBusy] = useState('');
  const [error, setError] = useState('');
  useEffect(() => {
    if (!shared.account) return;
    let active = true;
    api.trainingMatches().then(rows => {
      if (active) [...rows].reverse().forEach(row => onMatch({ id: row.id, status: row.status, createdAt: row.createdAt,
        startedAt: row.startedAt ?? undefined, count: row.count, difficulty: row.difficulty, seconds: row.timePerProblemSeconds }));
    }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [shared.account?.id]);
  async function start(id: string) {
    if (!shared.authorized()) return;
    setBusy(id); setError('');
    try {
      const result = await api.startMatch(id);
      onMatch({ ...matches.find(match => match.id === id), ...result });
      openMatch(id);
    } catch (e) { setError((e as Error).message); } finally { setBusy(''); }
  }
  async function begin(event: FormEvent) {
    event.preventDefault();
    if (!shared.authorized()) return;
    setBusy('create'); setError('');
    try {
      const created = await api.createMatch(count, difficulty, seconds);
      onMatch({ ...created, count, difficulty, seconds });
      const started = await api.startMatch(created.id);
      onMatch({ ...created, ...started, count, difficulty, seconds });
      openMatch(created.id);
    } catch (e) { setError((e as Error).message); } finally { setBusy(''); }
  }
  return <>
    <PageTitle eyebrow="ВЫБОР РЕЖИМА" title="Твой раунд. Твои правила." description="Настрой одиночную тренировку и переходи сразу к задачам." />
    <div className="training-layout"><section className="panel setup-panel"><div className="section-head"><h2><Target size={21} />Тренировка</h2><span className="badge green">Один игрок</span></div>
      <form onSubmit={begin} className="form-stack"><Field label="Сложность"><select value={difficulty} onChange={e => setDifficulty(e.target.value as Difficulty)}>{(['Easy', 'Medium', 'Hard'] as const).map(value => <option key={value} value={value}>{difficultyNames[value]}</option>)}</select></Field>
      <div className="form-grid"><Field label="Количество задач"><input type="number" min={1} max={100} required value={count} onChange={e => setCount(Number(e.target.value))} /></Field><Field label="Время на каждую задачу, сек."><input type="number" min={1} max={86400} required value={seconds} onChange={e => setSeconds(Number(e.target.value))} /></Field></div>
      <div className="time-presets">{[180, 300, 600, 900].map(value => <button type="button" className={seconds === value ? 'selected' : ''} key={value} onClick={() => setSeconds(value)}>{value / 60} мин</button>)}</div>
      <Notice>Задачи выбираются случайно без повторов. Каждая получает отдельный таймер. Закрытие вкладки не останавливает текущее время.</Notice>
      {error && <Notice error>{error}</Notice>}
      <Button type="submit" busy={busy === 'create'} disabled={!!busy}><Play size={17} />Начать тренировку<ArrowRight size={17} /></Button></form>
    </section><section className="panel setup-panel"><div className="section-head"><h2>Ваши тренировки</h2><span className="badge">{matches.length}</span></div>
      {!matches.length && <p className="muted">Здесь появятся созданные тренировки и сохранённые результаты.</p>}
      <div className="session-history">{matches.map(match => <article key={match.id}><div><strong>{match.count || '—'} задач · {match.difficulty ? difficultyNames[match.difficulty] : 'Тренировка'}</strong><p className="muted">{match.status === 'Created' ? 'Готова к запуску' : match.status === 'InProgress' ? 'Можно продолжить' : 'Результат сохранён'}{match.createdAt && ` · ${date(match.createdAt)}`}</p></div>
      {match.status === 'Created' ? <Button variant="secondary" busy={busy === match.id} disabled={!!busy} onClick={() => void start(match.id)}>Начать</Button> : <Button variant="secondary" onClick={() => openMatch(match.id)}>{match.status === 'InProgress' ? 'Продолжить' : 'Результат'}<ArrowRight size={15} /></Button>}</article>)}</div>
    </section></div>
  </>;
}
