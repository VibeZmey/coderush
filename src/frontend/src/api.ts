export type Account = { id: string; email: string | null; userName: string | null; isAdmin?: boolean };
export type MatchResult = { id: string; status: string; createdAt?: string; startedAt?: string };
export type RunResult = { id: string; status: 'Pending' | 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'; createdAt: string;
  completedAt?: string | null; failureCode?: string | null; result?: {
    verdict: string; standardOutput: string | null; standardError: string | null; compilationOutput: string | null;
    exitCode: number | null; exitSignal: number | null; cpuTimeSeconds: number | null; wallTimeSeconds: number | null; memoryKilobytes: number | null;
  } | null };
export type TrainingLanguage = { id: string; programmingLanguageId: number; name: string; fileExtension: string; template: string; draftSource: string | null; draftInput: string | null };
export type SubmissionResult = {
  id: string; status: string; verdict: string | null; passedTests: number; totalTests: number;
  failedTestNumber: number | null; failureCode: string | null; compilationOutput: string | null;
  exitCode: number | null; exitSignal: number | null; submittedAt: string;
  sample: { input: string; expectedOutput: string; standardOutput: string | null; standardError: string | null } | null;
};
export type TrainingProblem = { id: string; position: number; status: string; outcome: string | null; title: string; descriptionMarkdown: string; inputDescriptionMarkdown: string | null; outputDescriptionMarkdown: string | null; constraintsMarkdown: string | null; explanationMarkdown: string | null; cpuTimeLimitSeconds: number; memoryLimitKilobytes: number; startedAt: string | null; submissionDeadlineAt: string | null; languages: TrainingLanguage[]; samples: { input: string; expectedOutput: string }[] };
export type TrainingTaskResult = { id: string; position: number; title: string; status: string; outcome: string | null; startedAt: string | null; closedAt: string | null; spentSeconds: number; runCount: number };
export type TrainingMatch = { id: string; createdAt: string; mode: string; status: string; difficulty: Difficulty; count: number; timePerProblemSeconds: number; serverTime: string; startedAt: string | null; finishedAt: string | null; completedAt: string | null; finishReason: string | null; currentProblem: TrainingProblem | null; result: { solved: number; timedOut: number; skipped: number; abandoned: number; started: number; notStarted: number; workSeconds: number; problems: TrainingTaskResult[] } };
export type PracticeProblem = { id: string; key: string; title: string; difficulty: Difficulty; topics: string[]; solved: boolean };
export type PracticeWorkspace = Pick<PracticeProblem, 'difficulty' | 'topics' | 'solved'> & {
  problem: TrainingProblem; latestSubmissionId: string | null;
};
export type ProblemResult = { id: string; versionId: string; versionNumber: number; status: string; createdAt: string };
export type ProblemSummary = { id: string; key: string; versionId: string; versionNumber: number; title: string; difficulty: Difficulty; status: 'Draft' | 'Published' | 'Retired'; testCount: number; languageCount: number; createdAt: string; isPractice: boolean; topics: string[] };
export type ProblemPage = { items: ProblemSummary[]; total: number; page: number; pageSize: number };
export type ProblemDetails = Omit<ProblemInput, 'key' | 'title' | 'difficulty' | 'publish' | 'isPractice' | 'topics'> & { summary: ProblemSummary };
export function problemToInput(problem: ProblemDetails): ProblemInput {
  return { key: problem.summary.key, title: problem.summary.title, difficulty: problem.summary.difficulty, publish: problem.summary.status === 'Published',
    isPractice: problem.summary.isPractice, topics: problem.summary.topics,
    descriptionMarkdown: problem.descriptionMarkdown, inputDescriptionMarkdown: problem.inputDescriptionMarkdown ?? '',
    outputDescriptionMarkdown: problem.outputDescriptionMarkdown ?? '', constraintsMarkdown: problem.constraintsMarkdown ?? '', explanationMarkdown: problem.explanationMarkdown ?? '',
    cpuTimeLimitSeconds: problem.cpuTimeLimitSeconds, wallTimeLimitSeconds: problem.wallTimeLimitSeconds, memoryLimitKilobytes: problem.memoryLimitKilobytes, outputLimitKilobytes: problem.outputLimitKilobytes,
    languages: problem.languages, testCases: problem.testCases };
}
export type Difficulty = 'Easy' | 'Medium' | 'Hard';
export type ProblemInput = {
  key: string; title: string; descriptionMarkdown: string; difficulty: Difficulty; publish: boolean; isPractice?: boolean; topics?: string[];
  inputDescriptionMarkdown: string; outputDescriptionMarkdown: string; constraintsMarkdown: string; explanationMarkdown: string;
  cpuTimeLimitSeconds: number; wallTimeLimitSeconds: number; memoryLimitKilobytes: number; outputLimitKilobytes: number;
  languages: { programmingLanguageId: number; template: string }[];
  testCases: { input: string; expectedOutput: string; isSample: boolean }[];
};
export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}

const statusMessages: Record<number, string> = {
  400: 'Проверьте заполненные поля.', 401: 'Войдите в аккаунт, чтобы продолжить.',
  403: 'Для этого действия необходима роль администратора.', 404: 'Объект не найден или недоступен вашему аккаунту.',
  409: 'Действие недоступно в текущем состоянии или такой объект уже существует.', 429: 'Слишком много запросов. Попробуйте позже.',
};
const identityMessages: Record<string, string> = {
  DuplicateEmail: 'Эта почта уже зарегистрирована.', DuplicateUserName: 'Это имя пользователя уже занято.',
  InvalidUserName: 'Имя пользователя может содержать латинские буквы, цифры и символы - . _ @ +.',
  InvalidEmail: 'Укажите корректную электронную почту.', PasswordTooShort: 'Пароль должен содержать не менее 10 символов.',
  PasswordRequiresNonAlphanumeric: 'Добавьте в пароль специальный символ.', PasswordRequiresDigit: 'Добавьте в пароль цифру.',
  PasswordRequiresLower: 'Добавьте в пароль строчную латинскую букву.', PasswordRequiresUpper: 'Добавьте в пароль заглавную латинскую букву.',
};
const businessMessages: Record<string, string> = {
  'A solution for this task is already being checked.': 'Решение этой задачи уже проверяется. Дождитесь результата.',
  'Only a created match can be started.': 'Эта тренировка уже запущена или завершена.',
  'Not enough published problems for the selected difficulty. Choose fewer problems or another difficulty.': 'Опубликованных задач этой сложности недостаточно. Уменьшите количество или выберите другую сложность.',
  'This action is not available for the current training state.': 'Действие недоступно для текущего состояния тренировки.',
  'A problem with this key already exists.': 'Задача с таким ключом уже существует.',
  'Select existing, enabled languages without duplicates.': 'Выберите включённые языки без повторений.',
  'This language is not available for the match problem.': 'Этот язык недоступен для назначенной задачи.',
  'Code can only be run for an active match problem before its deadline.': 'Отправка доступна только для активной задачи до окончания её времени.',
};

export async function request<T>(path: string, body?: unknown, method?: 'PUT'): Promise<T> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 20000);
  try {
    const response = await fetch(`/api${path}`, {
      method: method || (body === undefined ? 'GET' : 'POST'), credentials: 'same-origin', signal: controller.signal,
      headers: body === undefined ? { Accept: 'application/json' } : { Accept: 'application/json', 'Content-Type': 'application/json' },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });
    if (!response.ok) {
      const data = await response.json().catch(() => null);
      // Do not surface infrastructure details or untrusted HTML returned by a proxy.
      const rawDetail = data?.detail || data?.message;
      const detail = response.status < 500 && (businessMessages[rawDetail] || rawDetail || (Array.isArray(data?.errors) ? data.errors.map((x: { code: string; description: string }) => identityMessages[x.code] || x.description).join(' ') : ''));
      if (response.status === 401 && path !== '/auth/login') window.dispatchEvent(new Event('session-expired'));
      throw new ApiError(response.status, detail || statusMessages[response.status] || 'Сервис временно недоступен. Попробуйте ещё раз.');
    }
    return response.status === 204 ? undefined as T : await response.json();
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new Error(controller.signal.aborted ? 'Сервер не ответил вовремя. Проверьте состояние операции перед повтором.' : 'Не удалось соединиться с сервером. Проверьте подключение.');
  } finally { clearTimeout(timeout); }
}

export const api = {
  me: () => request<Account>('/auth/me'),
  login: (email: string, password: string, rememberMe: boolean) => request<void>('/auth/login', { email, password, rememberMe }),
  register: (email: string, password: string, userName: string) => request<Account>('/auth/register', { email, password, userName }),
  logout: () => request<void>('/auth/logout', {}),
  createMatch: (count: number, difficulty: Difficulty, timePerProblemSeconds: number) => request<MatchResult>('/matches/training', { count, difficulty, timePerProblemSeconds }),
  startMatch: (matchId: string) => request<MatchResult>('/matches/start', { matchId }),
  practiceCatalog: () => request<PracticeProblem[]>('/practice'),
  previewPractice: (id: string) => request<PracticeWorkspace>(`/practice/${encodeURIComponent(id)}`),
  openPractice: (id: string) => request<PracticeWorkspace>(`/practice/${encodeURIComponent(id)}/open`, {}),
  savePracticeDraft: (matchProblemId: string, programmingLanguageId: number, sourceCode: string, standardInput: string) =>
    request<void>(`/practice/drafts/${encodeURIComponent(matchProblemId)}`, { matchProblemId, programmingLanguageId, sourceCode, standardInput }, 'PUT'),
  trainingMatches: () => request<TrainingMatch[]>('/matches'),
  trainingMatch: (id: string) => request<TrainingMatch>(`/matches/${encodeURIComponent(id)}`),
  nextTrainingProblem: (id: string) => request<TrainingMatch>(`/matches/${encodeURIComponent(id)}/next`, {}),
  leaveTrainingMatch: (id: string) => request<TrainingMatch>(`/matches/${encodeURIComponent(id)}/leave`, {}),
  saveTrainingDraft: (id: string, matchProblemId: string, programmingLanguageId: number, sourceCode: string, standardInput: string) =>
    request<void>(`/matches/${encodeURIComponent(id)}/draft`, { matchProblemId, programmingLanguageId, sourceCode, standardInput }, 'PUT'),
  run: (matchProblemId: string, programmingLanguageId: number, sourceCode: string, standardInput: string) => request<RunResult>('/code/run', { matchProblemId, programmingLanguageId, sourceCode, standardInput }),
  codeRun: (id: string) => request<RunResult>(`/code/runs/${encodeURIComponent(id)}`),
  submit: (matchProblemId: string, programmingLanguageId: number, sourceCode: string) =>
    request<SubmissionResult>('/code/submissions', { matchProblemId, programmingLanguageId, sourceCode }),
  submission: (id: string) => request<SubmissionResult>(`/code/submissions/${encodeURIComponent(id)}`),
  createProblem: (problem: ProblemInput) => request<ProblemResult>('/problems', problem),
  problems: (params: URLSearchParams) => request<ProblemPage>(`/problems?${params}`),
  problem: (id: string) => request<ProblemDetails>(`/problems/${encodeURIComponent(id)}`),
  updateProblem: (id: string, baseVersionId: string, problem: ProblemInput) => request<ProblemResult>(`/problems/${encodeURIComponent(id)}`, { baseVersionId, problem }, 'PUT'),
  publishProblem: async (id: string, baseVersionId: string): Promise<ProblemResult> => {
    const path = `/problems/${encodeURIComponent(id)}`;
    const current = await request<ProblemDetails>(path);
    if (current.summary.versionId !== baseVersionId)
      throw new ApiError(409, 'Задача изменилась. Обновите список перед публикацией.');
    return request<ProblemResult>(path, { baseVersionId, problem: { ...problemToInput(current), publish: true } }, 'PUT');
  },
  importProblems: (problems: ProblemInput[]) => request<{ problems: ProblemResult[] }>('/problems/import', { problems }),
};

export const uuidPattern = '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}';
export function validateCode(source: string, input: string): string | null {
  if (!source.trim()) return 'Напишите код перед запуском.';
  if (source.includes('\0') || input.includes('\0')) return 'Код и ввод не должны содержать нулевой символ.';
  if (new TextEncoder().encode(source).length > 65536) return 'Размер кода превышает 64 КиБ.';
  if (new TextEncoder().encode(input).length > 1048576) return 'Размер ввода превышает 1 МиБ.';
  return null;
}
