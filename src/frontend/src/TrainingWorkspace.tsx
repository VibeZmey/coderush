import { readSession, writeSession, removeSession } from './sessionStorage';
import { useEffect, useRef, useState } from 'react';
import { ArrowRight, Clock3, Layers3, LogOut, Play, Target, Trophy } from './icons';
import { api, ApiError, type Account, type PracticeProblem, type PracticeWorkspace, type RunResult, type SubmissionResult, type TrainingMatch, validateCode } from './api';
import { difficultyNames } from './catalog';
import { Button, Empty, Modal, Notice, Success } from './components';
import { PracticeDrawer, practiceHref } from './PracticeCatalog';
import { formatDuration, remainingSeconds } from './trainingTime';
import SubmissionOutput from './SubmissionOutput';
import WorkbenchPanes, { type WorkbenchPane, type WorkbenchTab } from './WorkbenchPanes';

const outcomeNames: Record<string, string> = { Solved: 'Решена', TimedOut: 'Время истекло', Skipped: 'Пропущена', Abandoned: 'Не завершена' };
type Buffers = Record<number, { source: string; input: string }>;

export default function TrainingWorkspace({ matchId, account, theme, notify, onRun, practice = false, authorized }: {
  practice?: boolean; authorized: () => boolean;
  matchId: string; account: Account | null; theme: 'dark' | 'light'; notify: (message: string) => void; onRun: (run: RunResult) => void;
}) {
  const [view, setView] = useState<TrainingMatch | null>(null);
  const workbench = useRef<HTMLDivElement>(null);
  const [drawer, setDrawer] = useState(false);
  const [practiceItems, setPracticeItems] = useState<PracticeProblem[] | null>(null);
  const [catalogError, setCatalogError] = useState('');
  const [catalogRetry, setCatalogRetry] = useState(0);
  const [practiceView, setPracticeView] = useState<PracticeWorkspace | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [running, setRunning] = useState(false);
  const [leave, setLeave] = useState(false);
  const [buffers, setBuffers] = useState<Buffers>({});
  const [languageId, setLanguageId] = useState(0);
  const [clock, setClock] = useState(Date.now());
  const [mobilePane, setMobilePane] = useState<WorkbenchPane>('statement');
  const [consoleTab, setConsoleTab] = useState<WorkbenchTab>('input');
  const [lastRun, setLastRun] = useState<RunResult | null>(null);
  const [pollError, setPollError] = useState('');
  const [submission, setSubmission] = useState<SubmissionResult | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submissionError, setSubmissionError] = useState('');
  const checking = submitting || !!submission && ['Pending', 'Queued', 'Running'].includes(submission.status);
  const executing = running || !!lastRun && ['Pending', 'Queued', 'Running'].includes(lastRun.status);
  const loadedTask = useRef('');
  const receivedAt = useRef(Date.now());
  const busyRef = useRef(false);
  const expiredTask = useRef('');
  const finished = useRef(false);
  function apply(next: TrainingMatch) {
    setView(previous => {
      if (previous && Date.parse(previous.serverTime) > Date.parse(next.serverTime)) return previous;
      receivedAt.current = Date.now();
      finished.current = next.status === 'Completed';
      return next;
    });
  }
  async function load() {
    try { apply(await api.trainingMatch(matchId)); }
    catch (e) { setError((e as Error).message); }
  }
  useEffect(() => {
    if (!matchId || (!account && !practice)) return;
    let active = true;
    if (practice) {
      void (account ? api.openPractice(matchId) : api.previewPractice(matchId)).then(next => { if (active) setPracticeView(next); })
        .catch(e => { if (active) setError(e.message); });
      return () => { active = false; };
    }
    const refresh = () => api.trainingMatch(matchId).then(next => { if (active) { apply(next); setError(''); } })
      .catch(e => { if (active) setError(e.message); });
    void refresh();
    const poll = setInterval(() => { if (!busyRef.current && !finished.current) void refresh(); }, 5000);
    return () => { active = false; clearInterval(poll); };
  }, [account?.id, matchId, practice]);
  useEffect(() => {
    if (practice || view?.status === 'Completed') return;
    const timer = setInterval(() => setClock(Date.now()), 500);
    return () => clearInterval(timer);
  }, [view?.status, practice]);
  useEffect(() => {
    if (!practice) return;
    let active = true;
    setCatalogError('');
    void api.practiceCatalog().then(items => { if (active) setPracticeItems(items); })
      .catch(e => { if (active) setCatalogError(e.message); });
    return () => { active = false; };
  }, [practice, account?.id, catalogRetry]);
  const currentIndex = practiceItems?.findIndex(item => item.id === matchId) ?? -1;
  const nextPracticeId = currentIndex < 0 ? undefined : practiceItems?.[currentIndex + 1]?.id;
  const problem = practiceView?.problem ?? view?.currentProblem;
  const seconds = problem && view ? remainingSeconds(problem.submissionDeadlineAt, view.serverTime, receivedAt.current, clock) : 0;
  const editable = !!account && !!problem && problem.status === 'Active' && (practice || (seconds > 0 && view?.status === 'InProgress'));
  useEffect(() => {
    if (!problem || loadedTask.current === problem.id) return;
    loadedTask.current = problem.id;
    setLanguageId(problem.languages[0]?.programmingLanguageId || 0);
    setBuffers(Object.fromEntries(problem.languages.map(language => [language.programmingLanguageId,
      { source: language.draftSource ?? language.template, input: language.draftInput ?? '' }])));
    setLastRun(null); setConsoleTab('input');
    setSubmission(null); setSubmissionError('');
    setPollError('');
    if (!account) return;
    let active = true;
    const previousRun = readSession(`code-run:${account?.id}:${problem.id}`);
    if (previousRun) void api.codeRun(previousRun).then(result => { if (active) setLastRun(result); })
      .catch(() => { if (active) removeSession(`code-run:${account?.id}:${problem.id}`); });
    const previousSubmission = practiceView?.latestSubmissionId ?? readSession(`submission:${account?.id}:${problem.id}`);
    if (previousSubmission) void api.submission(previousSubmission).then(result => { if (active) setSubmission(result); })
      .catch(() => { if (active) removeSession(`submission:${account?.id}:${problem.id}`); });
    return () => { active = false; };
  }, [problem?.id]);
  useEffect(() => {
    if (!lastRun || !['Pending', 'Queued', 'Running'].includes(lastRun.status)) return;
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    const poll = async () => {
      try {
        const result = await api.codeRun(lastRun.id);
        if (!active) return;
        setLastRun(result); setPollError('');
        if (['Completed', 'Failed', 'Cancelled'].includes(result.status)) return;
      } catch (e) {
        if (!active) return;
        setPollError((e as Error).message);
        if (e instanceof ApiError && [401, 403, 404].includes(e.status)) return;
      }
      if (active) timer = setTimeout(() => void poll(), 1500);
    };
    timer = setTimeout(() => void poll(), 750);
    return () => { active = false; clearTimeout(timer); };
  }, [lastRun?.id, lastRun?.status]);
  useEffect(() => {
    if (!submission || !['Pending', 'Queued', 'Running'].includes(submission.status)) return;
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    const poll = async () => {
      try {
        const result = await api.submission(submission.id);
        if (!active) return;
        setSubmission(result); setSubmissionError('');
        if (['Completed', 'Failed', 'Cancelled'].includes(result.status)) {
          if (practice && result.verdict === 'Accepted') setPracticeView(current => current && { ...current, solved: true });
          if (!practice) void load();
          return;
        }
      } catch (e) {
        if (!active) return;
        setSubmissionError((e as Error).message);
        if (e instanceof ApiError && [401, 403, 404].includes(e.status)) return;
      }
      if (active) timer = setTimeout(() => void poll(), 1000);
    };
    timer = setTimeout(() => void poll(), 500);
    return () => { active = false; clearTimeout(timer); };
  }, [submission?.id, submission?.status]);
  useEffect(() => {
    if (!practice && problem?.status === 'Active' && seconds === 0 && expiredTask.current !== problem.id) {
      expiredTask.current = problem.id;
      void load();
    }
  }, [problem?.id, problem?.status, seconds, practice]);
  const buffer = buffers[languageId] || { source: '', input: '' };
  function saveDraft(taskId: string) {
    return practice
      ? api.savePracticeDraft(taskId, languageId, buffer.source, buffer.input)
      : api.saveTrainingDraft(matchId, taskId, languageId, buffer.source, buffer.input);
  }
  async function save() {
    if (!problem || !editable) return;
    await saveDraft(problem.id);
  }
  useEffect(() => {
    if (!editable || !loadedTask.current || !buffers[languageId] || busy) return;
    const taskId = problem!.id;
    let active = true;
    const timer = setTimeout(() => {
      void saveDraft(taskId)
        .catch(e => { if (active && !(e instanceof ApiError && e.status === 409)) { setError(e.message); } });
    }, 600);
    return () => { active = false; clearTimeout(timer); };
  }, [matchId, problem?.id, languageId, buffer.source, buffer.input, editable, busy]);
  function change(value: Partial<{ source: string; input: string }>) {
    setBuffers(current => ({ ...current, [languageId]: { ...buffer, ...value } }));
  }
  async function goToPractice(id?: string) {
    if (busyRef.current) return;
    if (id === matchId) { setDrawer(false); return; }
    setDrawer(false);
    busyRef.current = true;
    setBusy(true);
    setError('');
    try {
      await save();
      if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        const animation = workbench.current?.animate([{ opacity: 1 }, { opacity: 0 }], {
          duration: 200, easing: 'cubic-bezier(0.22, 1, 0.36, 1)', fill: 'forwards',
        });
        await animation?.finished;
      }
      location.hash = id ? practiceHref(id) : '#practice';
    } catch (e) { setError((e as Error).message); }
    finally { busyRef.current = false; setBusy(false); }
  }
  async function action(kind: 'next' | 'leave') {
    setError(''); setBusy(true); busyRef.current = true;
    try {
      try { await save(); } catch (e) { if (!(e instanceof ApiError && e.status === 409)) throw e; }
      apply(await (kind === 'next' ? api.nextTrainingProblem(matchId) : api.leaveTrainingMatch(matchId)));
      setLeave(false);
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); busyRef.current = false; }
  }
  async function run() {
    if (!problem || executing) return;
    const issue = validateCode(buffer.source, buffer.input);
    if (issue) { setError(issue); return; }
    setRunning(true); setError('');
    try {
      const result = await api.run(problem.id, languageId, buffer.source, buffer.input);
      writeSession(`code-run:${account?.id}:${problem.id}`, result.id);
      setPollError('');
      setLastRun(result); setConsoleTab('run'); setMobilePane('console'); onRun(result); notify('Код принят в очередь');
    } catch (e) { setError((e as Error).message); }
    finally { setRunning(false); }
  }
  async function submit() {
    if (!problem || !editable || checking || executing) return;
    const issue = validateCode(buffer.source, '');
    if (issue) { setError(issue); return; }
    setSubmitting(true); setError(''); setSubmissionError('');
    try {
      if (practice) await save();
      const result = await api.submit(problem.id, languageId, buffer.source);
      writeSession(`submission:${account?.id}:${problem.id}`, result.id);
      setSubmission(result); setConsoleTab('submission'); setMobilePane('console');
      notify('Решение отправлено на проверку');
    } catch (e) { setError((e as Error).message); }
    finally { setSubmitting(false); }
  }
  if (!practice && (!account || !matchId)) return <section className="panel"><Empty icon={<Target size={28} />} title="Начните тренировку">Выберите режим и настройки в разделе <a href="#training">Тренировки</a>.</Empty></section>;
  if (practice && !practiceView) return <section className={`panel session-loading${error ? ' practice-enter' : ' practice-loading'}`}>{error ? <><Notice error>{error}</Notice><a href="#practice">К каталогу</a></> : 'Загружаем задачу…'}</section>;
  if (!practice && !view) return <section className="panel session-loading">{error ? <Notice error>{error}</Notice> : 'Загружаем тренировку…'}</section>;
  if (view?.status === 'Completed') return <>
    <div className="page-heading"><div><span className="eyebrow">РЕЗУЛЬТАТ ТРЕНИРОВКИ</span><h1>{view.finishReason === 'Abandoned' ? 'Тренировка завершена досрочно.' : 'Тренировка завершена.'}</h1><p>Результат и история задач сохранены в вашем аккаунте.</p></div><Trophy size={38} /></div>
    <div className="stats-grid">{[['Решено', view.result.solved], ['Время истекло', view.result.timedOut], ['Пропущено', view.result.skipped], ['Не завершено', view.result.abandoned]].map(([label, value]) => <section className="panel session-stat" key={label}><span>{label}</span><strong>{value}</strong></section>)}</div>
    <section className="panel session-results"><p>Начато: {view.result.started} из {view.count} · Время работы над задачами: {formatDuration(view.result.workSeconds)}</p>
      <Notice>Задача засчитывается после прохождения всех тестов. Обычный запуск кода не влияет на результат.</Notice>
      {submission && <SubmissionOutput submission={submission} />}
      <div className="session-table-wrap"><table><thead><tr><th>№</th><th>Задача</th><th>Итог</th><th>Время</th><th>Запуски</th></tr></thead><tbody>{view.result.problems.map(task => <tr key={task.id}><td>{task.position}</td><td>{task.title}</td><td>{task.startedAt ? outcomeNames[task.outcome || ''] || '—' : 'Не открывалась'}</td><td>{formatDuration(task.spentSeconds)}</td><td>{task.runCount}</td></tr>)}</tbody></table></div>
      <a className="button primary" href="#training">Новая тренировка <ArrowRight size={16} /></a></section>
  </>;
  if (!problem) return <section className="panel session-loading"><Notice>В этой сессии пока нет активной задачи. Создайте и начните новую тренировку.</Notice><div className="inline-actions"><a href="#training">К тренировкам</a>{view?.status === 'InProgress' && <Button variant="secondary" busy={busy} onClick={() => void action('leave')}>Завершить старую сессию</Button>}</div></section>;
  return <div ref={workbench} className={practice ? "training-workbench practice-workbench practice-enter" : "training-workbench"}>
    <header className="workbench-topbar">
      <a className="arena-brand workbench-brand" href="#home" aria-label="CodeRush, главная"><img src="/logo.svg" alt="" /><span>code<span>rush</span><i>_</i></span></a>{practice
        ? <Button variant="secondary" className="practice-list-trigger" aria-haspopup="dialog" aria-expanded={drawer}
            disabled={busy} onClick={() => setDrawer(true)}><Layers3 size={16} />Список задач</Button>
        : <a href="#training" className="workbench-mode title-4">Тренировка</a>}
      <div className="workbench-runbar"><span className="caption-2">{practiceView
        ? [difficultyNames[practiceView.difficulty], ...practiceView.topics, ...(practiceView.solved ? ['Решена'] : [])].join(' · ')
        : `Задача ${problem.position} из ${view?.count}`}</span><div><Button variant="secondary" busy={executing} disabled={!editable || busy || checking || !languageId} onClick={run}><Play size={16} />Запустить</Button><Button busy={checking} disabled={!editable || busy || executing || !languageId} onClick={submit}>Проверить решение<ArrowRight size={16} /></Button></div></div>
      {practice ? <div className="workbench-management"><div className="workbench-session-actions">
        <Button variant="secondary" disabled={busy} onClick={() => void goToPractice()}><LogOut size={16} />Выйти</Button>
        <Button variant="secondary" disabled={busy || !nextPracticeId} onClick={() => void goToPractice(nextPracticeId)}>Следующая задача<ArrowRight size={16} /></Button>
      </div></div> : <div className="workbench-management">
      <div className={`session-timer ${seconds < 30 ? 'urgent' : ''}`} role="timer" aria-label="Осталось времени"><Clock3 size={18} />{formatDuration(seconds)}</div>
      <div className="workbench-session-actions"><Button variant="secondary" disabled={busy || executing || checking} onClick={() => setLeave(true)}><LogOut size={16} /><span>Завершить</span></Button><Button variant="secondary" busy={busy} disabled={executing || checking || view?.status === 'Finalizing'} onClick={() => void action('next')}>{editable ? 'Пропустить' : 'Следующая задача'}<ArrowRight size={16} /></Button></div>
      </div>}
    </header>
    {(error || !editable || (practice && submission?.verdict === 'Accepted')) && <div className="workbench-notices">
      {error && <Notice error>{error}</Notice>}
      {practice && !account && <Notice>Войдите, чтобы запускать код, отправлять решения и сохранять черновики. <button type="button" className="text-button" onClick={authorized}>Войти</button></Notice>}
      {practice && submission?.verdict === 'Accepted' && <div className="practice-completion">
        <div role="status"><Success>Задача решена. Все тесты пройдены.</Success></div>
        <div className="inline-actions">
          <Button variant="secondary" disabled={busy} onClick={() => void goToPractice()}>К списку задач</Button>
          {nextPracticeId && <Button disabled={busy} onClick={() => void goToPractice(nextPracticeId)}>Следующая задача<ArrowRight size={16} /></Button>}
        </div>
      </div>}
      {!practice && !editable && <Notice>{problem.status === 'Finalizing' ? 'Ожидаем результат проверки решения.' : `${outcomeNames[problem.outcome || ''] || 'Время задачи закончилось'}. Можно перейти к следующей задаче.`}</Notice>}
    </div>}
    <WorkbenchPanes problem={problem} theme={theme} buffer={buffer} change={change} editable={editable} busy={busy}
      languageId={languageId} setLanguageId={setLanguageId} mobilePane={mobilePane} setMobilePane={setMobilePane}
      consoleTab={consoleTab} setConsoleTab={setConsoleTab} executing={executing} checking={checking}
      lastRun={lastRun} submission={submission} pollError={pollError} submissionError={submissionError} />
    {drawer && <PracticeDrawer items={(practiceItems ?? []).map(item => item.id === matchId && practiceView?.solved ? { ...item, solved: true } : item)}
      currentId={matchId} onSelect={id => void goToPractice(id)} onClose={() => setDrawer(false)}
      busy={busy} signedIn={!!account} loading={!practiceItems} error={catalogError} retry={() => setCatalogRetry(n => n + 1)} />}
    {leave && <Modal title="Завершить тренировку досрочно?" onClose={() => setLeave(false)}><p>Результаты сохранятся. Текущая и оставшиеся задачи будут отмечены как незавершённые.</p><div className="inline-actions"><Button variant="secondary" disabled={busy} onClick={() => setLeave(false)}>Продолжить тренировку</Button><Button busy={busy} onClick={() => void action('leave')}>Завершить</Button></div></Modal>}
  </div>;
}
