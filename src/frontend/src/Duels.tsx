import { useEffect, useRef, useState, type FormEvent, type CSSProperties } from 'react';
import { api, ApiError, request, validateCode, type Account, type Difficulty, type RunResult, type SubmissionResult } from './api';
import { difficultyNames } from './catalog';
import { Button, Field, Modal, Notice } from './components';
import { DuelRankBadge, DuelRatingCard, ratingDelta, resultNames, type DuelRating, type DuelResult } from './DuelRating';
import { readSession, removeSession, writeSession } from './sessionStorage';
import { formatDuration, remainingSeconds } from './trainingTime';
import { ArrowRight, Check, Clock3, Code2, Play, Trophy } from './icons';
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
  startsAt: string | null; deadline: string | null; opponent: string | null; opponentRating: number | null;
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
const initials = (name: string) => name.slice(0, 2).toUpperCase();
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
    <div className="page-heading"><div><span className="eyebrow">СОРЕВНОВАНИЕ 1 НА 1</span><h1>Дуэли</h1>
      <p>Одна задача на двоих. Поднимай рейтинг, побеждая соперников своего уровня.</p></div></div>
    {state && <DuelRatingCard rating={state.rating} />}
    {pollError && <Notice error>{pollError}</Notice>}
    {error && <Notice error>{error}</Notice>}
    {account && !state && !pollError && <p className="muted" role="status">Загружаем дуэли…</p>}
    {confirming && <Modal title="Матч найден" className="duel-dialog duel-ready-dialog">
      <div className="duel-confirmation" data-countdown={match.status === 'Countdown'}>
        <span className="eyebrow">{match.ranked ? 'РЕЙТИНГОВАЯ ДУЭЛЬ' : 'СВОЙ МАТЧ'} · 1 НА 1</span>
        <div className="duel-ready-players">
          <ReadyPlayer name={account.userName ?? 'Ты'} ready={match.accepted || match.status === 'Countdown'} own />
          <span className="duel-versus" aria-hidden="true">VS</span>
          <ReadyPlayer name={match.opponent ?? 'Соперник'} ready={match.opponentAccepted || match.status === 'Countdown'} />
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
      <section className="panel setup-panel"><h2>Рейтинговая игра</h2>
        <p className="muted">Соперник с близким рейтингом. Сложность зависит от рейтинга пары. На задачу — 15 минут.</p>
        {state?.searching ? <><p role="status">{state.searchMessage ?? 'Ищем соперника…'}</p>
          <Button variant="secondary" busy={busy} onClick={() => void act('/duels/cancel')}>Отменить поиск</Button></>
          : <Button busy={busy} disabled={!!account && !state} onClick={() => void act('/duels/search')}>Играть</Button>}
      </section>
      {invitePending && invite ? <section className="panel setup-panel"><h2>Приглашение в дуэль</h2>
        <p className="muted">После входа оба игрока подтверждают участие. Рейтинг в этом матче не меняется.</p>
        <Button busy={busy} onClick={() => void act('/duels/custom/' + encodeURIComponent(invite) + '/join')}>Присоединиться</Button>
      </section> : <section className="panel setup-panel"><h2>Свой матч</h2>
        <p className="muted">Пригласи друга по ссылке и сыграйте без изменения рейтинга.</p>
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
    {terminal && !visible && !invitePending && !state?.searching && <Button variant="secondary" className="duel-reopen-result"
      onClick={() => { removeSession(dismissedKey); setDismissed(null); }}>Итог последнего матча</Button>}
  </>;
}

function ReadyPlayer({ name, ready, own = false }: { name: string; ready: boolean; own?: boolean }) {
  return <div className="duel-ready-player" data-ready={ready}>
    <span className="avatar" aria-hidden="true">{initials(name)}</span>
    <strong>{name}</strong>
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
      <Trophy size={24} aria-hidden="true" />
      <strong>{match.result ? resultNames[match.result] : 'Матч завершён'}</strong>
      <span>{match.ranked ? ratingDelta(match.delta) + ' Elo' : 'Свой матч'}</span>
      {match.problem && <p>{match.problem.title}</p>}
    </div>
    <div className="duel-result-players">
      <div><strong>Ты</strong><span>{attemptLabel(match.ownAttempts[0])}</span></div>
      <div><strong>{match.opponent ?? 'Соперник'}</strong><span>{attemptLabel(match.opponentAttempts[0])}</span></div>
    </div>
    <div className="duel-result-actions">
      <Button variant="secondary" busy={busy} onClick={() => void toggleCode()}>
        <Code2 size={16} />{showCode ? 'Скрыть код' : 'Код соперника'}
      </Button>
      <a className="button secondary" href={'#opponent?match=' + encodeURIComponent(match.id)} onClick={onClose}>
        Профиль соперника <ArrowRight size={16} />
      </a>
    </div>
    {showCode && (error ? <Notice error>{error}</Notice> : code === null
      ? <p className="muted">Соперник не сохранил код в этом матче.</p>
      : code && <section className="duel-result-code"><h3>{code.solved ? 'Зачтённое решение' : 'Последний сохранённый код'} · {code.language}</h3>
        <pre tabIndex={0}><code>{code.sourceCode}</code></pre></section>)}
    <Button onClick={onClose}>К выбору матча</Button>
  </Modal>;
}

export function DuelOpponentProfile({ matchId }: { matchId: string }) {
  const [profile, setProfile] = useState<{ userName: string; rating: DuelRating }>();
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    request<{ userName: string; rating: DuelRating }>('/duels/' + encodeURIComponent(matchId) + '/opponent-profile')
      .then(value => { if (active) setProfile(value); })
      .catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [matchId]);
  return <>
    <div className="page-heading"><div>{profile && <div className="profile-avatar" aria-hidden="true">{initials(profile.userName)}</div>}
      <span className="eyebrow">ПРОФИЛЬ СОПЕРНИКА</span>
      <h1>{profile?.userName ?? 'Соперник'}</h1></div><a className="button secondary" href="#duels">К дуэлям</a></div>
    {error ? <Notice error>{error}</Notice> : profile ? <DuelRatingCard rating={profile.rating} compact />
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
            {initials(match.opponent ?? 'Соперник')}
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
