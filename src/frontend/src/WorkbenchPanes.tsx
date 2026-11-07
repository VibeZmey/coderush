import { lazy, Suspense, useRef, useState } from 'react';
import Markdown from 'react-markdown';
import type { RunResult, SubmissionResult, TrainingProblem } from './api';
import { Notice } from './components';
import CodeRunOutput from './CodeRunOutput';
import SubmissionOutput from './SubmissionOutput';
import ResizeHandle from './ResizeHandle';
import './training-workspace.css';

const CodeEditor = lazy(() => import('./CodeEditor'));
export type WorkbenchPane = 'statement' | 'code' | 'console';
export type WorkbenchTab = 'input' | 'run' | 'submission';
type Buffer = { source: string; input: string };
type Problem = Pick<TrainingProblem, 'title' | 'descriptionMarkdown' | 'inputDescriptionMarkdown' |
  'outputDescriptionMarkdown' | 'constraintsMarkdown' | 'explanationMarkdown' | 'cpuTimeLimitSeconds' |
  'memoryLimitKilobytes' | 'samples'> & { languages: { programmingLanguageId: number; name: string }[] };

export default function WorkbenchPanes({
  problem, theme, buffer, change, editable, busy, languageId, setLanguageId, mobilePane, setMobilePane,
  consoleTab, setConsoleTab, executing, checking, lastRun, submission, pollError, submissionError, verdictOnly,
}: {
  problem: Problem; theme: 'dark' | 'light'; buffer: Buffer; change: (value: Partial<Buffer>) => void;
  editable: boolean; busy: boolean; languageId: number; setLanguageId: (id: number) => void;
  mobilePane: WorkbenchPane; setMobilePane: (pane: WorkbenchPane) => void;
  consoleTab: WorkbenchTab; setConsoleTab: (tab: WorkbenchTab) => void;
  executing: boolean; checking: boolean; lastRun?: RunResult | null; submission?: SubmissionResult | null;
  pollError?: string; submissionError?: string; verdictOnly?: boolean;
}) {
  const [statementWidth, setStatementWidth] = useState(42);
  const [editorHeight, setEditorHeight] = useState(65);
  const columns = useRef<HTMLDivElement>(null);
  const rows = useRef<HTMLDivElement>(null);
  return <>
    <div className="workbench-mobile-tabs" role="tablist" aria-label="Рабочая область">{([{ id: 'statement', label: 'Условие' }, { id: 'code', label: 'Код' }, { id: 'console', label: 'Консоль' }] as const).map(pane => <button key={pane.id} role="tab" id={`mobile-${pane.id}`} aria-selected={mobilePane === pane.id} aria-controls={`workbench-${pane.id}`} onClick={() => setMobilePane(pane.id)}>{pane.label}</button>)}</div>
    <div ref={columns} className="workbench-columns" data-mobile-pane={mobilePane} style={{ gridTemplateColumns: `minmax(0, ${statementWidth}fr) 8px minmax(0, ${100 - statementWidth}fr)` }}>
      <section id="workbench-statement" className="workbench-panel workbench-statement" aria-label="Условие задачи">
        <div className="workbench-panel-heading workbench-statement-heading"><span className="title-4">Условие</span><h1 className="title-4">{problem.title}</h1></div>
        <div className="workbench-scroll problem-markdown" tabIndex={0} aria-label="Текст условия"><p className="workbench-limits caption-5">CPU {problem.cpuTimeLimitSeconds} с · Память {Math.round(problem.memoryLimitKilobytes / 1024)} МиБ</p>
          <Markdown>{problem.descriptionMarkdown}</Markdown>
          {problem.inputDescriptionMarkdown && <><h3>Входные данные</h3><Markdown>{problem.inputDescriptionMarkdown}</Markdown></>}
          {problem.outputDescriptionMarkdown && <><h3>Выходные данные</h3><Markdown>{problem.outputDescriptionMarkdown}</Markdown></>}
          {problem.constraintsMarkdown && <><h3>Ограничения</h3><Markdown>{problem.constraintsMarkdown}</Markdown></>}
          {problem.samples.length > 0 && <h3>Примеры</h3>}
          {problem.samples.map((sample, index) => <div className="sample-io" key={index}><div><small>ВВОД</small><pre>{sample.input || '(пусто)'}</pre></div><div><small>ВЫВОД</small><pre>{sample.expectedOutput || '(пусто)'}</pre></div></div>)}
          {problem.explanationMarkdown && <><h3>Пояснение</h3><Markdown>{problem.explanationMarkdown}</Markdown></>}
        </div>
      </section>
      <ResizeHandle container={columns} axis="horizontal" value={statementWidth} onChange={setStatementWidth} defaultValue={42} label="Ширина условия" />
      <div ref={rows} className="workbench-right" style={{ gridTemplateRows: `minmax(0, ${editorHeight}fr) 8px minmax(0, ${100 - editorHeight}fr)` }}>
        <section id="workbench-code" className="workbench-panel workbench-code" aria-label="Код решения">
          <div className="workbench-panel-heading"><h2 className="title-4">Код</h2><select aria-label="Язык программирования" value={languageId} disabled={busy} onChange={e => setLanguageId(Number(e.target.value))}>{problem.languages.map(language => <option key={language.programmingLanguageId} value={language.programmingLanguageId}>{language.name}</option>)}</select></div>
          <div className="workbench-editor"><Suspense fallback={<div className="editor-loading">Загружаем редактор…</div>}><CodeEditor value={buffer.source} onChange={source => change({ source })} languageId={languageId} theme={theme} editable={editable && !busy} /></Suspense></div>
        </section>
        <ResizeHandle container={rows} axis="vertical" value={editorHeight} onChange={setEditorHeight} defaultValue={65} label="Высота редактора" />
        <section id="workbench-console" className="workbench-panel workbench-console" aria-label="Ввод и результаты">
          <div className="workbench-console-tabs" role="tablist" aria-label="Консоль">{([{ id: 'input', label: 'Входные данные' }, { id: 'run', label: 'Запуск' }, { id: 'submission', label: 'Проверка' }] as const).map(tab => <button key={tab.id} id={`console-tab-${tab.id}`} role="tab" aria-selected={consoleTab === tab.id} aria-controls="console-content" onClick={() => setConsoleTab(tab.id)}>{tab.label}{((tab.id === 'run' && executing) || (tab.id === 'submission' && checking)) && <span className="workbench-pending" aria-label="Выполняется" />}</button>)}</div>
          <div id="console-content" className="workbench-console-content" role="tabpanel" aria-labelledby={`console-tab-${consoleTab}`}>
            <textarea hidden={consoleTab !== 'input'} aria-label="Ваши входные данные" className="workbench-stdin" value={buffer.input} onChange={e => change({ input: e.target.value })} readOnly={!editable || busy} spellCheck={false} placeholder="Введи данные для пробного запуска" />
            {consoleTab === 'run' && <>{pollError && <Notice error>{pollError}</Notice>}{lastRun ? <CodeRunOutput run={lastRun} /> : <p className="muted body-6">Запусти код, чтобы увидеть результат на своих входных данных.</p>}</>}
            {consoleTab === 'submission' && <>{submissionError && <Notice error>{submissionError}</Notice>}{submission ? <SubmissionOutput submission={submission} verdictOnly={verdictOnly} /> : <p className="muted body-6">Отправь решение на проверку, чтобы увидеть результаты тестов.</p>}</>}
          </div>
        </section>
      </div>
    </div>
  </>;
}
