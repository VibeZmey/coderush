import type { RunResult } from './api';
import { Notice } from './components';

const statusNames: Record<RunResult['status'], string> = {
  Pending: 'Ожидает отправки', Queued: 'В очереди', Running: 'Выполняется',
  Completed: 'Завершён', Failed: 'Ошибка запуска', Cancelled: 'Отменён',
};
const verdictNames: Record<string, string> = {
  Succeeded: 'Код выполнен', TimeLimitExceeded: 'Превышено время выполнения', CompilationError: 'Ошибка компиляции',
  RuntimeError: 'Ошибка выполнения', MemoryLimitExceeded: 'Превышен лимит памяти', OutputLimitExceeded: 'Превышен лимит вывода',
};
const failureNames: Record<string, string> = {
  ExecutionTimeout: 'Сервис выполнения не вернул результат вовремя. Запустите код ещё раз.',
  DispatchUncertain: 'Не удалось подтвердить отправку кода. Запуск не повторялся автоматически. Попробуйте ещё раз.',
  Judge0Error: 'Сервис выполнения не смог обработать код. Попробуйте ещё раз.',
  Judge0Rejected: 'Сервис выполнения отклонил запуск. Проверьте настройки языка и лимиты выполнения.',
};

export default function CodeRunOutput({ run }: { run: RunResult }) {
  const output = run.result;
  return <section className="run-output" aria-label="Результат запуска">
    <div role="status"><strong>{output ? verdictNames[output.verdict] || output.verdict : statusNames[run.status]}</strong></div>
    {run.status === 'Failed' && <Notice error>{failureNames[run.failureCode || ''] || 'Не удалось выполнить код. Попробуйте ещё раз.'}</Notice>}
    {output && <>
      <p className="muted">{output.cpuTimeSeconds != null && <>CPU: {output.cpuTimeSeconds} сек. · </>}
        {output.wallTimeSeconds != null && <>Время: {output.wallTimeSeconds} сек. · </>}
        {output.memoryKilobytes != null && <>Память: {output.memoryKilobytes} КиБ · </>}
        Код выхода: {output.exitCode ?? '—'}{output.exitSignal != null && <> · Сигнал: {output.exitSignal}</>}</p>
      <div className="sample-io"><div><small>STDOUT</small><pre>{output.standardOutput || '(пусто)'}</pre></div></div>
      {output.standardError && <div className="sample-io"><div><small>STDERR</small><pre>{output.standardError}</pre></div></div>}
      {output.compilationOutput && <div className="sample-io"><div><small>ВЫВОД КОМПИЛЯТОРА</small><pre>{output.compilationOutput}</pre></div></div>}
    </>}
  </section>;
}
