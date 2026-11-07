import type { SubmissionResult } from './api';
import { Notice } from './components';

const verdictNames: Record<string, string> = {
  Accepted: 'Все тесты пройдены. Задача решена!', WrongAnswer: 'Неверный ответ',
  TimeLimitExceeded: 'Превышено время выполнения', CompilationError: 'Ошибка компиляции', RuntimeError: 'Ошибка выполнения',
  MemoryLimitExceeded: 'Превышен лимит памяти', OutputLimitExceeded: 'Превышен лимит вывода',
};

export default function SubmissionOutput({ submission: s, verdictOnly = false }: { submission: SubmissionResult; verdictOnly?: boolean }) {
  const pending = ['Pending', 'Queued', 'Running'].includes(s.status);
  return <section className="run-output" aria-label="Результат проверки решения">
    {(!verdictOnly || s.verdict === 'Accepted') && <div role="status"><strong>{s.passedTests}/{s.totalTests} тестов пройдено</strong></div>}
    <Notice error={!pending && s.verdict !== 'Accepted'}>{pending ? 'Проверяем решение…' :
      s.status === 'Failed' ? 'Ошибка сервиса проверки. Решение не засчитано; попробуйте отправить снова.' :
      s.status === 'Cancelled' ? 'Проверка отменена.' : verdictNames[s.verdict || ''] || 'Проверка завершена.'}
      {s.failedTestNumber != null && <> Тест №{s.failedTestNumber}.</>}
      {s.exitCode != null && s.exitCode !== 0 && <> Код выхода: {s.exitCode}.</>}
      {s.exitSignal != null && <> Сигнал: {s.exitSignal}.</>}
    </Notice>
    {s.compilationOutput && <div className="sample-io"><div><small>ВЫВОД КОМПИЛЯТОРА</small><pre>{s.compilationOutput}</pre></div></div>}
    {s.sample && <>
      <div className="sample-io"><div><small>ВВОД ПРИМЕРА</small><pre>{s.sample.input || '(пусто)'}</pre></div>
        <div><small>ОЖИДАЕМЫЙ ВЫВОД</small><pre>{s.sample.expectedOutput || '(пусто)'}</pre></div></div>
      <div className="sample-io"><div><small>ВЫВОД ПРОГРАММЫ</small><pre>{s.sample.standardOutput || '(пусто)'}</pre></div></div>
      {s.sample.standardError && <div className="sample-io"><div><small>ОШИБКА ВЫПОЛНЕНИЯ</small><pre>{s.sample.standardError}</pre></div></div>}
    </>}
  </section>;
}
