import type { ProblemInput } from './api';

function object(value: unknown, path: string): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error(`${path}: ожидается объект.`);
  return value as Record<string, unknown>;
}
function keys(value: Record<string, unknown>, allowed: string[], path: string) {
  const unknown = Object.keys(value).find(k => !allowed.includes(k));
  if (unknown) throw new Error(`${path}: неизвестное поле «${unknown}».`);
}
function text(value: unknown, path: string, maxBytes: number, required = false): string {
  if (!required && value == null) return '';
  if (typeof value !== 'string') throw new Error(`${path}: ожидается строка.`);
  if (value.includes('\0') || new TextEncoder().encode(value).length > maxBytes) throw new Error(`${path}: недопустимый символ или превышен лимит ${maxBytes} байт.`);
  if (required && !value.trim()) throw new Error(`${path}: заполните поле.`);
  return value;
}
function number(value: unknown, fallback: number, path: string, integer = false): number {
  if (value === undefined) return fallback;
  if (typeof value !== 'number' || !Number.isFinite(value) || value <= 0 || (integer && (!Number.isInteger(value) || value > 2147483647))) throw new Error(`${path}: ожидается положительное ${integer ? 'целое ' : ''}число.`);
  return value;
}
function boolean(value: unknown, path: string): boolean {
  if (value === undefined) return false;
  if (typeof value !== 'boolean') throw new Error(`${path}: ожидается true или false.`);
  return value;
}

export function parseProblemFile(json: string, expectedPool?: boolean): ProblemInput[] {
  let data: unknown;
  try { data = JSON.parse(json.replace(/^\uFEFF/, '')); } catch { throw new Error('Не удалось прочитать JSON. Проверьте кавычки, запятые и скобки.'); }
  let list: unknown[];
  if (Array.isArray(data)) list = data;
  else {
    const root = object(data, 'JSON');
    if ('problems' in root) { keys(root, ['problems'], 'JSON'); if (!Array.isArray(root.problems)) throw new Error('problems: ожидается массив задач.'); list = root.problems; }
    else list = [root];
  }
  if (list.length < 1 || list.length > 60) throw new Error('В файле должно быть от 1 до 60 задач.');
  const parsed = list.map((value, index): ProblemInput => {
    const path = `Задача ${index + 1}`; const p = object(value, path);
    keys(p, ['key', 'title', 'descriptionMarkdown', 'inputDescriptionMarkdown', 'outputDescriptionMarkdown', 'constraintsMarkdown', 'explanationMarkdown', 'difficulty', 'publish', 'isPractice', 'topics', 'cpuTimeLimitSeconds', 'wallTimeLimitSeconds', 'memoryLimitKilobytes', 'outputLimitKilobytes', 'languages', 'testCases'], path);
    const key = text(p.key, `${path}.key`, 128, true);
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(key)) throw new Error(`${path}.key: используйте строчные латинские буквы, цифры и дефисы.`);
    const title = text(p.title, `${path}.title`, 1024, true);
    if (title.length > 256) throw new Error(`${path}.title: не больше 256 символов.`);
    if (!['Easy', 'Medium', 'Hard'].includes(p.difficulty as string)) throw new Error(`${path}.difficulty: ожидается Easy, Medium или Hard.`);
    if (!Array.isArray(p.languages) || !p.languages.length) throw new Error(`${path}.languages: нужен хотя бы один язык.`);
    if (!Array.isArray(p.testCases) || !p.testCases.length) throw new Error(`${path}.testCases: нужен хотя бы один тест.`);
    const languages = p.languages.map((value, i) => {
      const l = object(value, `${path}.languages[${i}]`); keys(l, ['programmingLanguageId', 'template'], `${path}.languages[${i}]`);
      if (l.programmingLanguageId === undefined) throw new Error(`${path}.languages[${i}]: нужен programmingLanguageId.`);
      return { programmingLanguageId: number(l.programmingLanguageId, 0, `${path}.languages[${i}].programmingLanguageId`, true), template: text(l.template, `${path}.languages[${i}].template`, 65536) };
    });
    if (new Set(languages.map(l => l.programmingLanguageId)).size !== languages.length) throw new Error(`${path}: языки не должны повторяться.`);
    const isPractice = p.isPractice === undefined && expectedPool !== undefined
      ? expectedPool : boolean(p.isPractice, `${path}.isPractice`);
    if (expectedPool !== undefined && isPractice !== expectedPool)
      throw new Error(`${path}: пул в файле отличается от выбранной категории. Откройте нужную категорию и повторите импорт.`);
    const topics = p.topics === undefined ? [] : p.topics;
    if (!Array.isArray(topics) || topics.length > 10 || (isPractice && topics.length === 0) ||
        topics.some(topic => typeof topic !== 'string' || !topic.trim() || topic.trim().length > 40)) {
      throw new Error(`${path}.topics: укажите до 10 тем длиной от 1 до 40 символов и хотя бы одну тему для каталога.`);
    }
    return { key, title, isPractice, topics, difficulty: p.difficulty as ProblemInput['difficulty'], publish: boolean(p.publish, `${path}.publish`),
      descriptionMarkdown: text(p.descriptionMarkdown, `${path}.descriptionMarkdown`, 1048576, true),
      inputDescriptionMarkdown: text(p.inputDescriptionMarkdown, `${path}.inputDescriptionMarkdown`, 1048576),
      outputDescriptionMarkdown: text(p.outputDescriptionMarkdown, `${path}.outputDescriptionMarkdown`, 1048576),
      constraintsMarkdown: text(p.constraintsMarkdown, `${path}.constraintsMarkdown`, 1048576),
      explanationMarkdown: text(p.explanationMarkdown, `${path}.explanationMarkdown`, 1048576),
      cpuTimeLimitSeconds: number(p.cpuTimeLimitSeconds, 2, `${path}.cpuTimeLimitSeconds`), wallTimeLimitSeconds: number(p.wallTimeLimitSeconds, 10, `${path}.wallTimeLimitSeconds`),
      memoryLimitKilobytes: number(p.memoryLimitKilobytes, 128000, `${path}.memoryLimitKilobytes`, true), outputLimitKilobytes: number(p.outputLimitKilobytes, 1024, `${path}.outputLimitKilobytes`, true), languages,
      testCases: p.testCases.map((value, i) => {
        const t = object(value, `${path}.testCases[${i}]`); keys(t, ['input', 'expectedOutput', 'isSample'], `${path}.testCases[${i}]`);
        if (typeof t.input !== 'string' || typeof t.expectedOutput !== 'string') throw new Error(`${path}.testCases[${i}]: нужны строки input и expectedOutput (могут быть пустыми).`);
        return { input: text(t.input, `${path}.testCases[${i}].input`, 1048576), expectedOutput: text(t.expectedOutput, `${path}.testCases[${i}].expectedOutput`, 1048576), isSample: boolean(t.isSample, `${path}.testCases[${i}].isSample`) };
      }) };
  });
  if (new Set(parsed.map(p => p.key)).size !== parsed.length) throw new Error('В файле повторяются ключи задач. Для каждой задачи нужен уникальный key.');
  return parsed;
}
