import Avatar from './Avatar';
import { useEffect, useRef, useState, type FormEvent, type CSSProperties } from 'react';
import { api, ApiError, request, validateCode, type Account, type Difficulty, type RunResult, type SubmissionResult } from './api';
import { difficultyNames } from './catalog';
import { AdminTag, Button, Field, Modal, Notice } from './components';
import { DuelRankBadge, DuelRatingCard, ratingDelta, resultNames, type DuelRating, type DuelResult } from './DuelRating';
import { readSession, writeSession } from './sessionStorage';
import { formatDuration, remainingSeconds } from './trainingTime';
import { ArrowRight, Check, CircleHelp, Clock3, Code2, Play, Trophy } from './icons';
import { MatchHistory } from './Profile';
import WorkbenchPanes, { type WorkbenchPane, type WorkbenchTab } from './WorkbenchPanes';

const difficulties: Difficulty[] = ['Easy', 'Medium', 'Hard'];
const readInvite = () => new URLSearchParams(location.hash.split('?')[1]).get('invite');

type Attempt = { id: string; status: string; verdict: string | null };
type Problem = {
  id: string; title: string; description: string; input: string | null; output: string | null;
  constraints: string | null; explanation: string | null; cpuTimeLimitSeconds: number; memoryLimitKilobytes: number;
  languages: { id: number; name: string; template: string }[];
  samples: { input: string; expectedOutput: string }[];
};
type Duel = {
  id: string; ranked: boolean; status: 'Created' | 'AwaitingAcceptance' | 'Countdown' | 'InProgress' | 'Finalizing' | 'Completed';
  startsAt: string | null; deadline: string | null; opponent: string | null; opponentIsAdmin: boolean; opponentRating: number | null;
  opponentRank: DuelRating['rank'] | null; problem: Problem | null;
  ownAttempts: Attempt[]; opponentAttempts: Attempt[]; result: DuelResult | null; delta: number;
  acceptBy: string | null; accepted: boolean; opponentAccepted: boolean;
};
type State = { serverTime: string; searching: boolean; searchMessage: string | null; rating: DuelRating; match: Duel | null };
const isChecking = (attempt: { status: string } | undefined) => !!attempt && ['Pending', 'Queued', 'Running'].includes(attempt.status);
const verdictNames: Record<string, string> = {
  Accepted: 'Решение принято', WrongAnswer: 'Неверный ответ', TimeLimitExceeded: 'Превышен лимит времени',
  MemoryLimitExceeded: 'Превышен лимит памяти', OutputLimitExceeded: 'Превышен лимит вывода',
  CompilationError: 'Ошибка компиляции', RuntimeError: 'Ошибка выполнения',
};
const attemptLabel = (attempt?: Attempt) => !attempt ? 'Ещё без отправок' : isChecking(attempt)
  ? 'Решение проверяется' : verdictNames[attempt.verdict ?? ''] ?? 'Сбой проверки';

export default function Duels({ account, authorized, theme }: {
  account: Account | null; authorized: () => boolean; theme: 'dark' | 'light';
}) {
  const [state, setState] = useState<(State & { receivedAt: number }) | null>(null);
  const [error, setError] = useState('');
  const [pollError, setPollError] = useState('');
  const [busy, setBusy] = useState(false);
  const [refresh, setRefresh] = useState(0);
  const dismissedKey = 'duel-dismissed:' + account?.id;
  const [dismissed, setDismissed] = useState(() => readSession(dismissedKey));
  const [difficulty, setDifficulty] = useState<Difficulty>('Easy');
  const [minutes, setMinutes] = useState(15);
  const [clock, setClock] = useState(Date.now());
  const [invite, setInvite] = useState(readInvite);
  const [copied, setCopied] = useState(false);
  const [showRules, setShowRules] = useState(false);
  const [historyMatchId, setHistoryMatchId] = useState<string>();
  const match = state?.match;
  const terminal = match?.status === 'Completed';
  const invitePending = !!invite && match?.id !== invite && (!match || terminal) && !state?.searching;
  const visible = match && (!terminal || (!state.searching && !invitePending && match.id !== dismissed));
  const accountId = account?.id;

  useEffect(() => {
    const updateInvite = () => setInvite(readInvite());
    window.addEventListener('hashchange', updateInvite);
    return () => window.removeEventListener('hashchange', updateInvite);
  }, []);

  useEffect(() => {
    if (!accountId) return;
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      let delay = 3000;
      try {
        const data = await request<State>('/duels/state', {});
        if (data.searching || (data.match && data.match.status !== 'Completed')) delay = 1000;
        if (data.match?.status === 'AwaitingAcceptance' || data.match?.status === 'Countdown') delay = 500;
        if (active) { setState({ ...data, receivedAt: Date.now() }); setPollError(''); }
      } catch (e) {
        if (active) setPollError((e as Error).message);
      } finally {
        if (active) timer = setTimeout(poll, delay);
      }
    }
    void poll();
    return () => { active = false; clearTimeout(timer); };
  }, [accountId, refresh]);

  useEffect(() => {
    if (state && invite && !state.match && readSession('duel-active-invite') === invite) {
      location.hash = 'duels';
    }
    if (invite && match?.id === invite) writeSession('duel-active-invite', invite);
  }, [state, invite, match?.id]);

  useEffect(() => {
    if (!match || !['AwaitingAcceptance', 'Countdown', 'InProgress'].includes(match.status)) return;
    const timer = setInterval(() => setClock(Date.now()), 250);
    return () => clearInterval(timer);
  }, [match?.status]);

  async function act(path: string, body: object = {}) {
    if (!authorized()) return;
    setBusy(true); setError('');
    try {
      await request<void>(path, body);
      if (match && terminal) dismiss(match.id);
      setRefresh(value => value + 1);
    } catch (e) {
      if (path.endsWith('/accept') && e instanceof ApiError && e.status === 409) setRefresh(value => value + 1);
      else setError((e as Error).message);
    }
    finally { setBusy(false); }
  }

  function create(event: FormEvent) {
    event.preventDefault();
    setCopied(false);
    void act('/duels/custom', { durationMinutes: minutes, difficulty: difficulties.indexOf(difficulty) + 1 });
  }

  function dismiss(id: string) {
    setDismissed(id);
    writeSession(dismissedKey, id);
  }

  function back() {
    if (match) dismiss(match.id);
    if (invite) location.hash = 'duels';
  }

  const seconds = visible && state
    ? remainingSeconds(match.status === 'AwaitingAcceptance' ? match.acceptBy : match.status === 'Countdown' ? match.startsAt : match.deadline, state.serverTime, state.receivedAt,
      Math.max(clock, state.receivedAt))
    : 0;
  const inviteLink = match ? new URL(location.pathname + location.search + '#duels?invite=' + match.id, location.origin).href : '';

  async function copyInvite() {
    try { await navigator.clipboard.writeText(inviteLink); setCopied(true); setError(''); }
    catch { setError('Выдели ссылку в поле и скопируй её вручную.'); }
  }

  if (visible && account && match.problem && (match.status === 'InProgress' || match.status === 'Finalizing')) {
    return <DuelWorkspace key={match.problem.id} accountId={account.id} theme={theme} match={match}
      problem={match.problem} seconds={seconds} connectionError={pollError}
      onSubmission={() => setRefresh(value => value + 1)} />;
  }

  const confirming = visible && account && (match.status === 'AwaitingAcceptance' || match.status === 'Countdown');
  return <>
    <header className="panel duel-hero">
      <div className="duel-hero-copy">
        <span className="eyebrow">АРЕНА CODERUSH</span>
        <h1>Дуэли</h1>
        <Button variant="secondary" onClick={() => setShowRules(true)}><CircleHelp size={16} aria-hidden="true" /> Правила</Button>
      </div>
    </header>
    {state && <DuelRatingCard rating={state.rating} label="Рейтинг" />}
    {showRules && !confirming && !(visible && terminal) && <DuelRulesDialog onClose={() => setShowRules(false)} />}
    {pollError && <Notice error>{pollError}</Notice>}
    {error && <Notice error>{error}</Notice>}
    {account && !state && !pollError && <p className="muted" role="status">Загружаем дуэли…</p>}
    {confirming && <Modal title="Матч найден" className="duel-dialog duel-ready-dialog">
      <div className="duel-confirmation" data-countdown={match.status === 'Countdown'}>
        <span className="eyebrow">{match.ranked ? 'РЕЙТИНГОВАЯ ДУЭЛЬ' : 'СВОЙ МАТЧ'} · 1 НА 1</span>
        <div className="duel-ready-players">
          <ReadyPlayer isAdmin={account.isAdmin} name={account.userName ?? 'Ты'} ready={match.accepted || match.status === 'Countdown'} own />
          <span className="duel-versus" aria-hidden="true">VS</span>
          <ReadyPlayer isAdmin={match.opponentIsAdmin} matchId={match.id} name={match.opponent ?? 'Соперник'} ready={match.opponentAccepted || match.status === 'Countdown'} />
        </div>
        <div className="duel-confirmation-clock" style={{ '--duel-time': `${seconds / (match.status === 'Countdown' ? 5 : 30) * 100}%` } as CSSProperties}>
          <span>{match.status === 'Countdown' ? 'До старта' : 'До конца подтверждения'}</span>
          <strong role="timer">{seconds}</strong>
        </div>
        {match.status === 'AwaitingAcceptance' && <Button busy={busy} disabled={match.accepted || !seconds}
          onClick={() => void act('/duels/' + match.id + '/accept')}>{match.accepted ? 'Ждём соперника' : 'Принять'}</Button>}
        <p className="muted caption-2">Код из редактора будет доступен сопернику после матча.</p>
        {(error || pollError) && <Notice error>{error || pollError}</Notice>}
      </div>
    </Modal>}
    {visible && terminal && <DuelResultDialog match={match} onClose={back} />}
    {visible && account && match.status === 'Created' ? <>
      <section className="panel duel-status">
        <div><span className="eyebrow">{match.ranked ? 'РЕЙТИНГОВАЯ ДУЭЛЬ' : 'ПОЛЬЗОВАТЕЛЬСКАЯ ДУЭЛЬ'}</span>
          <h2>Пригласи соперника</h2>
        </div>
        {match.status === 'Created' && <div className="duel-invite">
          <Field label="Ссылка на матч"><input readOnly value={inviteLink} onFocus={event => event.currentTarget.select()} /></Field>
          <div className="duel-invite-buttons">
            <Button onClick={() => void copyInvite()}>{copied ? 'Ссылка скопирована' : 'Скопировать ссылку'}</Button>
            <Button variant="secondary" busy={busy} onClick={() => void act('/duels/cancel')}>Закрыть лобби</Button>
          </div>
        </div>}
      </section>
    </> : !confirming && <div className="duel-actions">
      <section className="panel setup-panel duel-mode duel-mode-ranked">
        <div className="duel-mode-meta"><span className="duel-mode-icon"><Trophy size={22} aria-hidden="true" /></span>
          <span><Clock3 size={14} aria-hidden="true" /> 15 минут</span></div>
        <h2>Рейтинговая игра</h2>
        <p className="muted">Соперник с близким рейтингом. Сложность зависит от рейтинга пары.</p>
        {state?.searching ? <><p className="duel-search-status" role="status">{state.searchMessage ?? 'Ищем соперника…'}</p>
          <Button variant="secondary" busy={busy} onClick={() => void act('/duels/cancel')}>Отменить поиск</Button></>
          : <Button className="duel-search-button" busy={busy} disabled={!!account && !state} onClick={() => void act('/duels/search')}>Найти соперника</Button>}
      </section>
      {invitePending && invite ? <section className="panel setup-panel duel-mode">
        <div className="duel-mode-meta"><span className="duel-mode-icon"><Code2 size={22} aria-hidden="true" /></span><span>По приглашению</span></div>
        <h2>Приглашение в дуэль</h2>
        <p className="muted">После входа оба игрока подтверждают участие. Рейтинг в этом матче не меняется.</p>
        <Button busy={busy} onClick={() => void act('/duels/custom/' + encodeURIComponent(invite) + '/join')}>Присоединиться</Button>
      </section> : <section className="panel setup-panel duel-mode">
        <div className="duel-mode-meta"><span className="duel-mode-icon"><Code2 size={22} aria-hidden="true" /></span><span>По приглашению</span></div>
        <h2>Свой матч</h2>
        <p className="muted">Пригласи друга по ссылке</p>
        <form className="form-stack" onSubmit={create}>
          <Field label="Сложность"><select value={difficulty} onChange={event => setDifficulty(event.target.value as Difficulty)}>
            {difficulties.map(value => <option key={value} value={value}>{difficultyNames[value]}</option>)}
          </select></Field>
          <Field label="Длительность, мин"><input type="number" min={1} max={60} required value={minutes}
            onChange={event => setMinutes(Number(event.target.value))} /></Field>
          <Button type="submit" variant="secondary" busy={busy}>Создать лобби</Button>
        </form>
      </section>}
    </div>}
    {accountId && <MatchHistory key={accountId} id={accountId} duelsOnly latestMatchId={terminal ? match.id : undefined}
      onResult={game => setHistoryMatchId(game.id)} />}
    {historyMatchId && !confirming && !(visible && terminal) &&
      <HistoricalDuelResult key={historyMatchId} id={historyMatchId} onClose={() => setHistoryMatchId(undefined)} />}
  </>;
}

export function HistoricalDuelResult({ id, onClose }: { id: string; onClose: () => void }) {
  const [match, setMatch] = useState<Duel>();
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    request<Duel>(`/duels/${encodeURIComponent(id)}/result`)
      .then(value => { if (active) setMatch(value); })
      .catch(() => { if (active) setError('Не удалось загрузить итог дуэли. Закрой окно и попробуй снова.'); });
    return () => { active = false; };
  }, [id]);

  return match ? <DuelResultDialog match={match} onClose={onClose} /> :
    <Modal title="Итог дуэли" className="duel-dialog" onClose={onClose}>
      {error ? <Notice error>{error}</Notice> : <p role="status">Загружаем итог…</p>}
    </Modal>;
}

function DuelRulesDialog({ onClose }: { onClose: () => void }) {
  return <Modal title="Правила дуэлей" className="duel-dialog duel-rules-dialog" onClose={onClose}>
    <div className="duel-rules">
      <section>
        <h3>Режимы и поиск соперника</h3>
        <p>Рейтинговая дуэль длится 15 минут. Поиск выбирает игрока с ближайшим рейтингом в допустимом диапазоне. Сначала разница может быть до 100 очков. Каждые 15 секунд, которые оба игрока провели в очереди, добавляют по 50 очков к пределу, максимум до 500.</p>
        <p>Сложность задачи определяется по среднему рейтингу пары. Ниже 1200 выпадает лёгкая задача, от 1200 до 1399 включительно средняя, от 1400 сложная. В своём матче создатель выбирает сложность и время от 1 до 60 минут, затем отправляет сопернику ссылку. Свои матчи проходят без изменения рейтинга.</p>
      </section>
      <section>
        <h3>Подтверждение и старт</h3>
        <p>У обоих игроков есть 30 секунд, чтобы принять матч. Если кто-то не подтвердит участие, матч отменяется. После двух подтверждений начинается отсчёт в 5 секунд. Оба игрока получают одну и ту же задачу.</p>
      </section>
      <section>
        <h3>Отправка и проверка</h3>
        <p>Для результата учитываются решения, отправленные с начала матча до истечения времени. Запуск кода помогает проверить решение, а в зачёт идёт отправка на проверку. Когда появляется первое принятое решение или заканчивается время, система ждёт завершения проверок, которые могут повлиять на результат. При сбое такой проверки рейтинг не меняется.</p>
        <p>После завершения матча соперник сможет посмотреть твоё принятое решение или последний сохранённый код, если принятого решения нет.</p>
      </section>
      <section>
        <h3>Кто побеждает</h3>
        <ol>
          <li>Если задачу решил один игрок, побеждает он. Если решили оба, выигрывает тот, кто раньше отправил принятое решение. При одинаковом времени отправки засчитывается ничья.</li>
          <li>Если задачу не решил никто, сравнивается наибольшее число пройденных тестов за одну отправку.</li>
          <li>Если оба прошли одинаковое ненулевое число тестов, побеждает тот, кому потребовалось меньше отправок до первой попытки с таким результатом. Если и этот показатель совпадает или никто не прошёл ни одного теста, засчитывается ничья.</li>
        </ol>
      </section>
      <section>
        <h3>Рейтинг и ранги</h3>
        <p>Стартовый рейтинг составляет 1000 очков. Ранг Intern действует до 1199 очков включительно, Junior начинается с 1200, Middle с 1400, Senior с 1600.</p>
        <p>Базовое изменение рейтинга составляет 25 очков, если задачу решил только победитель, 23, если решили оба, 21 за победу по числу тестов и 20 за победу по числу попыток. Из базы вычитается число отправок победителя до первой принятой или первой лучшей попытки. Итоговое изменение составляет минимум 18 очков. Победитель получает столько же очков, сколько теряет проигравший. При ничьей рейтинг не меняется.</p>
      </section>
    </div>
  </Modal>;
}

function ReadyPlayer({ name, ready, matchId, own = false, isAdmin }: { isAdmin?: boolean; name: string; ready: boolean; matchId?: string; own?: boolean }) {
  return <div className="duel-ready-player" data-ready={ready}>
    <span className="avatar" aria-hidden="true"><Avatar name={name} matchId={matchId} /></span>
    <strong>{name}{isAdmin && <AdminTag />}</strong>
    <span className="duel-ready-state">{ready && <Check size={14} aria-hidden="true" />}{ready ? 'Принято' : own ? 'Твоё подтверждение' : 'Ожидает'}</span>
  </div>;
}

type OpponentCode = { sourceCode: string; language: string; solved: boolean };

function DuelResultDialog({ match, onClose }: { match: Duel; onClose: () => void }) {
  const [code, setCode] = useState<OpponentCode | null>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [showCode, setShowCode] = useState(false);

  async function toggleCode() {
    setShowCode(!showCode);
    if (showCode || code !== undefined) return;
    setBusy(true); setError('');
    try { setCode(await request<OpponentCode>('/duels/' + match.id + '/opponent-code')); }
    catch (e) {
      if (e instanceof ApiError && e.status === 404) setCode(null);
      else setError((e as Error).message);
    } finally { setBusy(false); }
  }

  return <Modal title="Итог дуэли" className="duel-dialog duel-result-dialog" onClose={onClose}>
    <div className="duel-result-summary" data-result={match.result ?? undefined}>
      <span className="duel-result-emblem"><Trophy size={26} aria-hidden="true" /></span>
      <span className="eyebrow">{match.ranked ? 'Рейтинговая дуэль' : 'Свой матч'}</span>
      <strong>{match.result ? resultNames[match.result] : 'Матч завершён'}</strong>
      {match.ranked && <span className="duel-result-delta">{ratingDelta(match.delta)} Elo</span>}
      {match.problem && <p>{match.problem.title}</p>}
    </div>
    <div className="duel-result-players">
      <div><span className="avatar" aria-hidden="true"><Avatar name="Ты" /></span><strong>Ты</strong><span>{attemptLabel(match.ownAttempts[0])}</span></div>
      <span className="duel-versus" aria-hidden="true">VS</span>
      <div><span className="avatar" aria-hidden="true"><Avatar name={match.opponent ?? 'Соперник'} matchId={match.id} /></span>
        <a href={'#opponent?match=' + encodeURIComponent(match.id)} onClick={onClose}>{match.opponent ?? 'Соперник'}{match.opponentIsAdmin && <AdminTag />}</a>
        <span>{attemptLabel(match.opponentAttempts[0])}</span></div>
    </div>
    <div className="duel-result-actions">
      <Button variant="secondary" busy={busy} onClick={() => void toggleCode()}>
        <Code2 size={16} />{showCode ? 'Скрыть код' : 'Код соперника'}
      </Button>
    </div>
    {showCode && (error ? <Notice error>{error}</Notice> : code === null
      ? <p className="muted">Соперник не сохранил код в этом матче.</p>
      : code && <section className="duel-result-code"><h3>{code.solved ? 'Зачтённое решение' : 'Последний сохранённый код'} · {code.language}</h3>
        <pre tabIndex={0}><code>{code.sourceCode}</code></pre></section>)}
  </Modal>;
}

export function DuelOpponentProfile({ matchId }: { matchId: string }) {
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    request<{ id: string }>('/duels/' + encodeURIComponent(matchId) + '/opponent-profile')
      .then(value => { if (active) location.replace(`#profile?id=${encodeURIComponent(value.id)}`); })
      .catch(() => { if (active) setError('Не удалось открыть профиль соперника.'); });
    return () => { active = false; };
  }, [matchId]);
  return <>
    {error ? <><Notice error>{error}</Notice><a className="button secondary" href="#duels">К дуэлям</a></>
      : <p className="muted" role="status">Загружаем профиль…</p>}
  </>;
}

function DuelWorkspace({ accountId, theme, match, problem, seconds, connectionError, onSubmission }: {
  accountId: string; theme: 'dark' | 'light'; match: Duel; problem: Problem; seconds: number;
  connectionError: string; onSubmission: () => void;
}) {
  const storage = 'duel-code:' + accountId + ':' + problem.id + ':';
  const [language, setLanguage] = useState(problem.languages[0]?.id ?? 0);
  const [buffers, setBuffers] = useState(() => Object.fromEntries(problem.languages.map(item => [item.id, {
    source: readSession(storage + item.id) ?? item.template,
    input: readSession(storage + 'input:' + item.id) ?? '',
  }])));
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [running, setRunning] = useState(false);
  const [submission, setSubmission] = useState<SubmissionResult>();
  const [lastRun, setLastRun] = useState<RunResult>();
  const [mobilePane, setMobilePane] = useState<WorkbenchPane>('statement');
  const [consoleTab, setConsoleTab] = useState<WorkbenchTab>('input');
  const draftTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const draftRequest = useRef(Promise.resolve());
  const nextDraft = useRef<{ programmingLanguageId: number; sourceCode: string } | undefined>(undefined);
  const last = match.ownAttempts[0];
  const lastId = last?.id;
  const lastStatus = last?.status;
  const runId = lastRun?.id;
  const runStatus = lastRun?.status;
  const editable = match.status === 'InProgress' && seconds > 0 && !connectionError;
  const buffer = buffers[language] ?? { source: '', input: '' };
  const checking = submitting || isChecking(last) || (!!submission && submission.id !== lastId && isChecking(submission));
  const executing = running || isChecking(lastRun);

  useEffect(() => () => clearTimeout(draftTimer.current), []);

  useEffect(() => {
    if (!lastId) return;
    let active = true;
    api.submission(lastId).then(value => {
      if (active) setSubmission(current => current && current.id !== lastId && Date.parse(current.submittedAt) > Date.parse(value.submittedAt) ? current : value);
    }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [lastId, lastStatus]);

  useEffect(() => {
    const previous = readSession(storage + 'run');
    if (!previous) return;
    let active = true;
    api.codeRun(previous).then(value => { if (active) setLastRun(current => current ?? value); }).catch(() => {});
    return () => { active = false; };
  }, [storage]);

  useEffect(() => {
    if (!runId || !isChecking(runStatus ? { status: runStatus } : undefined)) return;
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      try {
        const result = await api.codeRun(runId!);
        if (!active) return;
        setLastRun(result);
        if (!isChecking(result)) return;
      } catch (e) {
        if (active) setError((e as Error).message);
      }
      if (active) timer = setTimeout(poll, 1200);
    }
    timer = setTimeout(poll, 750);
    return () => { active = false; clearTimeout(timer); };
  }, [runId, runStatus]);

  function change(value: Partial<typeof buffer>) {
    const next = { ...buffer, ...value };
    setBuffers(current => ({ ...current, [language]: next }));
    if (value.source !== undefined) {
      writeSession(storage + language, next.source);
      nextDraft.current = { programmingLanguageId: language, sourceCode: next.source };
      if (draftTimer.current === undefined) {
        draftTimer.current = setTimeout(() => {
          draftTimer.current = undefined;
          draftRequest.current = draftRequest.current.then(async () => {
            const draft = nextDraft.current;
            nextDraft.current = undefined;
            if (draft) await request<void>('/duels/' + match.id + '/draft', draft, 'PUT');
          }).catch(() => { /* The local buffer is still available. */ });
        }, 350);
      }
    }
    if (value.input !== undefined) writeSession(storage + 'input:' + language, next.input);
  }

  async function run() {
    const issue = validateCode(buffer.source, buffer.input);
    if (issue) { setError(issue); return; }
    setRunning(true); setError('');
    try {
      const result = await api.run(problem.id, language, buffer.source, buffer.input);
      writeSession(storage + 'run', result.id);
      setLastRun(result); setConsoleTab('run'); setMobilePane('console');
    } catch (e) { setError((e as Error).message); }
    finally { setRunning(false); }
  }

  async function submit() {
    const issue = validateCode(buffer.source, '');
    if (issue) { setError(issue); return; }
    setSubmitting(true); setError('');
    try {
      setSubmission(await api.submit(problem.id, language, buffer.source));
      setConsoleTab('submission'); setMobilePane('console');
      onSubmission();
    } catch (e) { setError((e as Error).message); }
    finally { setSubmitting(false); }
  }

  const statement = {
    ...problem, descriptionMarkdown: problem.description, inputDescriptionMarkdown: problem.input,
    outputDescriptionMarkdown: problem.output, constraintsMarkdown: problem.constraints, explanationMarkdown: problem.explanation,
    languages: problem.languages.map(item => ({ programmingLanguageId: item.id, name: item.name })),
  };
  const opponentAttempt = match.opponentAttempts[0];
  const opponentActivity = !opponentAttempt ? 'idle' : isChecking(opponentAttempt) ? 'checking'
    : opponentAttempt.verdict === 'Accepted' ? 'accepted' : 'failed';
  return <div className="training-workbench duel-workbench">
    <header className="workbench-topbar">
      <a className="arena-brand workbench-brand" href="#home" aria-label="CodeRush, главная"><img src="/logo.svg" alt="" /><span>code<span>rush</span><i>_</i></span></a>
      <span className="workbench-mode title-4">{match.ranked ? 'Рейтинговая дуэль' : 'Своя дуэль'}</span>
      <div className="workbench-runbar"><div>
        <Button variant="secondary" busy={executing} disabled={!editable || checking || !language} onClick={() => void run()}><Play size={16} />Запустить</Button>
        <Button busy={checking} disabled={!editable || executing || !language} onClick={() => void submit()}>Проверить решение<ArrowRight size={16} /></Button>
      </div></div>
      <div className="workbench-management">
        <div className="duel-workbench-opponent">
          <span className="avatar duel-opponent-avatar" data-activity={opponentActivity} title={attemptLabel(opponentAttempt)} aria-hidden="true">
            <Avatar name={match.opponent ?? 'Соперник'} matchId={match.id} />
          </span>
          <span className="sr-only" role="status">{attemptLabel(opponentAttempt)}</span>
          <div className="duel-opponent-label"><span className="caption-4">Соперник</span>
            {match.opponentRank && match.opponentRating !== null && <DuelRankBadge rank={match.opponentRank} rating={match.opponentRating} />}
          </div>
        </div>
        <div className={`session-timer ${seconds < 30 ? 'urgent' : ''}`} role="timer" aria-label="Осталось времени"><Clock3 size={18} />{formatDuration(seconds)}</div>
      </div>
    </header>
    {(error || connectionError || match.status === 'Finalizing' || !seconds) && <div className="workbench-notices">
      {(error || connectionError) && <Notice error>{connectionError || error}</Notice>}
      {(match.status === 'Finalizing' || !seconds) && <Notice>Ожидаем результат проверки.</Notice>}
    </div>}
    <WorkbenchPanes problem={statement} theme={theme} buffer={buffer} change={change} editable={editable}
      busy={submitting || running} languageId={language} setLanguageId={setLanguage} mobilePane={mobilePane}
      setMobilePane={setMobilePane} consoleTab={consoleTab} setConsoleTab={setConsoleTab} executing={executing}
      checking={checking} lastRun={lastRun} submission={submission} verdictOnly />
  </div>;
}
