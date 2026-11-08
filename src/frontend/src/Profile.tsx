import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { ApiError, request, uuidPattern, type Account } from './api';
import { AdminTag, Button, Field, Modal, Notice } from './components';
import { LockKeyhole, Pencil, Trophy, User } from './icons';
import ProfileMedia from './ProfileMedia';
import { DuelRatingCard, ratingDelta, type DuelRating, type Rank } from './DuelRating';
import './profile.css';

type Profile = {
  id: string;
  userName: string | null;
  isAdmin: boolean;
  bio: string | null;
  registeredAt: string | null;
  today: string;
  rating: DuelRating;
  activity: Record<string, number>;
  practice: {
    solved: number; easy: number; medium: number; hard: number;
    acceptancePercent: number | null;
  };
};
type Game = {
  id: string; playedAt: string; mode: 'Training' | 'Ranked' | 'Custom';
  result: 'Win' | 'Loss' | 'Draw' | 'Completed' | 'Abandoned' | 'Unavailable';
  rating: number | null; delta: number | null; rank: Rank | null;
};
type History = { items: Game[]; hasMore: boolean };
type ImageKind = 'avatar' | 'background';
type Preview = { dataUrl: string; image: string };
const idPattern = new RegExp(`^${uuidPattern}$`);
const dateFormat = new Intl.DateTimeFormat('ru', { dateStyle: 'long', timeZone: 'UTC' });
const gameTime = new Intl.DateTimeFormat('ru', { dateStyle: 'medium', timeStyle: 'short' });
const monthFormat = new Intl.DateTimeFormat('ru', { month: 'short', timeZone: 'UTC' });
const modes = { Training: 'Тренировка', Ranked: 'Рейтинговая дуэль', Custom: 'Своя дуэль' };
const outcomes = {
  Win: 'Победа', Loss: 'Поражение', Draw: 'Ничья',
  Completed: 'Завершена', Abandoned: 'Прервана', Unavailable: 'Сбой проверки',
};
const imageKinds: ImageKind[] = ['avatar', 'background'];
const imageLabels = { avatar: 'Аватар', background: 'Обложка' };
const maxImageBytes = 5 * 1024 * 1024;

function mediaUrl(id: string, kind: ImageKind) {
  return `/api/profiles/${encodeURIComponent(id)}/media/${kind}`;
}

function useProfile(id: string) {
  const [profile, setProfile] = useState<Profile>();
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    if (!idPattern.test(id)) {
      setError('Профиль не найден.');
      return;
    }
    request<Profile>(`/profiles/${encodeURIComponent(id)}`)
      .then(value => { if (active) setProfile(value); })
      .catch(failure => { if (active) setError(failure.message); });
    return () => { active = false; };
  }, [id]);
  return { profile, error };
}

function Identity({ name, bio, registeredAt, primary = false, isAdmin = false }: {
  name: string; bio?: string | null; registeredAt?: string | null; primary?: boolean; isAdmin?: boolean;
}) {
  return <>
    {primary ? <h1>{name}{isAdmin && <AdminTag />}</h1> : <h2>{name}{isAdmin && <AdminTag />}</h2>}
    {bio && <p className="profile-bio">{bio}</p>}
    <p className="muted">{registeredAt ? `Участник с ${dateFormat.format(new Date(registeredAt))}` : 'Дата регистрации недоступна'}</p>
  </>;
}

export function ProfilePage({ id, own, notify, openTraining, renderDuelResult }: {
  id: string; own: boolean; notify: (text: string) => void;
  openTraining: (id: string) => void; renderDuelResult: (id: string, close: () => void) => ReactNode;
}) {
  const [selected, setSelected] = useState<Game>();
  const { profile, error } = useProfile(id);

  async function share() {
    try {
      const link = new URL(location.href);
      link.hash = `profile?id=${encodeURIComponent(id)}`;
      await navigator.clipboard.writeText(link.href);
      notify('Ссылка на профиль скопирована.');
    } catch {
      notify('Не удалось скопировать ссылку.');
    }
  }

  if (error) return <Notice error>{error}</Notice>;
  if (!profile) return <p role="status">Загружаем профиль…</p>;
  return <>
    <ProfileMedia avatar={mediaUrl(id, 'avatar')} cover={mediaUrl(id, 'background')} actions={<>
      {own && <a className="button secondary" href="#profile-settings"><Pencil size={16} />Редактировать профиль</a>}
      <Button variant="secondary" onClick={() => void share()}>Поделиться</Button>
    </>}>
      <Identity name={profile.userName ?? 'Игрок'} bio={profile.bio} registeredAt={profile.registeredAt} isAdmin={profile.isAdmin} primary />
    </ProfileMedia>
    <DuelRatingCard rating={profile.rating} compact />
    <PracticeStats stats={profile.practice} />
    <Activity profile={profile} />
    <MatchHistory id={id} onResult={game => {
      if (own && game.mode === 'Training') openTraining(game.id);
      else setSelected(game);
    }} />
    {selected && (own && selected.mode !== 'Training'
      ? renderDuelResult(selected.id, () => setSelected(undefined))
      : <Modal title="Итог матча" onClose={() => setSelected(undefined)}>
        <div className="form-stack">
          <p>{modes[selected.mode]} · <time dateTime={selected.playedAt}>{gameTime.format(new Date(selected.playedAt))}</time></p>
          <strong className="profile-result" data-result={selected.result}>{outcomes[selected.result]}</strong>
          {selected.rating !== null && selected.delta !== null && <p>Рейтинг {selected.rating} ({ratingDelta(selected.delta)})</p>}
        </div>
      </Modal>)}
  </>;
}

function PracticeStats({ stats }: { stats: Profile['practice'] }) {
  const acceptance = stats.acceptancePercent === null ? '—'
    : `${stats.acceptancePercent.toLocaleString('ru')} %`;
  return <section className="panel profile-section profile-practice">
    <h2>Решение задач</h2>
    <dl>
      <div><dt>Всего решено</dt><dd>{stats.solved}</dd></div>
      <div><dt>Лёгкие</dt><dd className="badge difficulty-Easy">{stats.easy}</dd></div>
      <div><dt>Средние</dt><dd className="badge difficulty-Medium">{stats.medium}</dd></div>
      <div><dt>Сложные</dt><dd className="badge difficulty-Hard">{stats.hard}</dd></div>
      <div><dt>Принято решений</dt><dd>{acceptance}</dd></div>
    </dl>

  </section>;
}

function Activity({ profile }: { profile: Profile }) {
  const grid = useRef<HTMLDivElement>(null);
  const [columns, setColumns] = useState(1);
  useEffect(() => {
    const element = grid.current;
    if (!element) return;
    const observer = new ResizeObserver(([entry]) => {
      setColumns(Math.max(1, Math.min(104, Math.floor((entry.contentRect.width - 28) / 24))));
    });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const last = new Date(`${profile.today}T00:00:00Z`);
  const lastRow = (last.getUTCDay() + 6) % 7;
  const count = (columns - 1) * 7 + lastRow + 1;
  const first = new Date(last);
  first.setUTCDate(first.getUTCDate() - count + 1);
  const days = Array.from({ length: count }, (_, slot) => {
    const date = new Date(first);
    date.setUTCDate(date.getUTCDate() + slot);
    const key = date.toISOString().slice(0, 10);
    return { date, key, count: profile.activity[key] ?? 0, slot };
  });
  const months = new Map<number, string>();
  for (const { date, slot } of days) {
    const column = Math.floor(slot / 7) + 2;
    if (date.getUTCDate() === 1 || !months.size) months.set(column, monthFormat.format(date));
  }

  return <section className="profile-section profile-activity">
    <div className="section-head"><h2>Недавняя активность</h2></div>
    <div className="activity-grid" ref={grid}>
      <div className="profile-heatmap" style={{ gridTemplateColumns: `24px repeat(${columns}, minmax(0, 1fr))` }}>
        {[...months].map(([column, label]) => <small key={column} className="muted heatmap-month"
          style={{ gridColumn: column, gridRow: 1 }}>{label}</small>)}
        {['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс'].map((day, index) => (
          <small key={day} className="muted" style={{ gridColumn: 1, gridRow: index + 2 }}>{day}</small>
        ))}
        {days.map(({ key, date, count, slot }) => {
          const label = `${dateFormat.format(date)} · игр: ${count}`;
          return <span key={key} className="heatmap-day" data-level={Math.min(count, 4)}
            style={{ gridColumn: Math.floor(slot / 7) + 2, gridRow: slot % 7 + 2 }}
            role="img" title={label} aria-label={label} />;
        })}
      </div>
    </div>
  </section>;
}

export function MatchHistory({ id, duelsOnly = false, latestMatchId, onResult }: {
  id: string; duelsOnly?: boolean; latestMatchId?: string; onResult?: (game: Game) => void;
}) {
  const [page, setPage] = useState(0);
  const [expanded, setExpanded] = useState(false);
  const [history, setHistory] = useState<History>();
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    setHistory(undefined);
    setError('');
    request<History>(`/profiles/${encodeURIComponent(id)}/matches?page=${page}&duelsOnly=${duelsOnly}`)
      .then(value => { if (active) setHistory(value); })
      .catch(failure => { if (active) setError(failure.message); });
    return () => { active = false; };
  }, [id, page, duelsOnly, latestMatchId]);

  return <section className="panel profile-section">
    <div className="section-head"><h2>{duelsOnly ? 'История дуэлей' : 'История матчей'}</h2></div>
    {error ? <Notice error>{error}</Notice> : !history ? <p role="status">Загружаем матчи…</p>
      : !history.items.length ? <p className="muted">Завершённых матчей пока нет.</p> : (
        <div className="table-scroll"><table className="profile-history">
          <thead><tr><th>Дата</th><th>Результат</th><th>Рейтинг</th><th>Режим</th></tr></thead>
          <tbody>{(expanded ? history.items : history.items.slice(0, 5)).map(game => <tr key={game.id}
            className={onResult ? 'profile-history-match' : undefined} onClick={onResult ? () => onResult(game) : undefined}>
            <td>{onResult ? <button type="button" className="profile-history-trigger"
              aria-label={`Открыть итог матча за ${gameTime.format(new Date(game.playedAt))}`}>
              <time dateTime={game.playedAt}>{gameTime.format(new Date(game.playedAt))}</time>
            </button> : <time dateTime={game.playedAt}>{gameTime.format(new Date(game.playedAt))}</time>}</td>
            <td><span className="profile-result" data-result={game.result}>{outcomes[game.result]}</span></td>
            <td>{game.rank && game.rating !== null && game.delta !== null ? <span className="profile-match-rating">
                <span className="duel-rank-badge" data-rank={game.rank} title={game.rank}><Trophy size={14} aria-label={game.rank} /> {game.rating}</span> ({ratingDelta(game.delta)})
              </span> : <span className="muted">—</span>}</td>
            <td>{modes[game.mode]}</td>
          </tr>)}</tbody>
        </table></div>
      )}
    {history && (expanded || history.items.length > 5 || history.hasMore) && <Button variant="secondary" className="profile-pagination" aria-expanded={expanded} onClick={() => {
      if (expanded) setPage(0);
      setExpanded(!expanded);
    }}>{expanded ? 'Свернуть историю' : 'Развернуть историю'}</Button>}
    {expanded && (page > 0 || history?.hasMore) && <div className="inline-actions profile-pagination">
      <Button variant="secondary" disabled={page === 0} onClick={() => setPage(page - 1)}>Назад</Button>
      <Button variant="secondary" disabled={!history?.hasMore} onClick={() => setPage(page + 1)}>Дальше</Button>
    </div>}
  </section>;
}

async function readPreview(file: File): Promise<HTMLImageElement> {
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type))
    throw new Error('Выбери JPEG, PNG или WebP.');
  if (file.size > maxImageBytes) throw new Error('Изображение должно весить не больше 5 МиБ.');
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error('Не удалось прочитать файл.'));
    reader.onload = async () => {
      const dataUrl = String(reader.result);
      const preview = new Image();
      preview.src = dataUrl;
      try {
        await preview.decode();
        if (preview.naturalWidth > 4096 || preview.naturalHeight > 4096 || preview.naturalWidth * preview.naturalHeight > 12_000_000)
          throw new Error();
        resolve(preview);
      } catch {
        reject(new Error('Выбери изображение до 4096 пикселей по каждой стороне и до 12 мегапикселей.'));
      }
    };
    reader.readAsDataURL(file);
  });
}

function ImageCrop({ kind, source, onClose, onApply }: {
  kind: ImageKind; source: HTMLImageElement; onClose: () => void; onApply: (preview: Preview) => void;
}) {
  const [zoom, setZoom] = useState(1);
  const [horizontal, setHorizontal] = useState(50);
  const [vertical, setVertical] = useState(50);
  const [error, setError] = useState('');
  const ratio = kind === 'avatar' ? 1 : 4;
  const width = Math.min(source.naturalWidth, source.naturalHeight * ratio) / zoom;
  const height = width / ratio;
  const left = (source.naturalWidth - width) * horizontal / 100;
  const top = (source.naturalHeight - height) * vertical / 100;

  function apply() {
    try {
      const canvas = document.createElement('canvas');
      canvas.width = Math.min(kind === 'avatar' ? 512 : 1600, Math.max(ratio, Math.round(width)));
      canvas.height = Math.round(canvas.width / ratio);
      const context = canvas.getContext('2d');
      if (!context) throw new Error();
      context.drawImage(source, left, top, width, height, 0, 0, canvas.width, canvas.height);
      const dataUrl = canvas.toDataURL('image/webp', 0.92);
      onApply({ dataUrl, image: dataUrl.slice(dataUrl.indexOf(',') + 1) });
    } catch {
      setError('Не удалось обрезать изображение. Попробуй выбрать другой файл.');
    }
  }

  return <Modal title={kind === 'avatar' ? 'Обрезать аватар' : 'Обрезать обложку'} onClose={onClose}>
    <div className="form-stack">
      <div className="profile-crop-frame" data-kind={kind} style={{ aspectRatio: ratio }}>
        <img src={source.src} alt="Предпросмотр обрезки" style={{
          width: `${source.naturalWidth / width * 100}%`,
          left: `${-left / width * 100}%`,
          top: `${-top / height * 100}%`,
        }} />
      </div>
      <Field label="Масштаб"><input type="range" min={1} max={4} step={0.01} value={zoom}
        onChange={event => setZoom(event.currentTarget.valueAsNumber)} /></Field>
      <Field label="Положение по горизонтали"><input type="range" min={0} max={100} value={horizontal}
        disabled={width >= source.naturalWidth} onChange={event => setHorizontal(event.currentTarget.valueAsNumber)} /></Field>
      <Field label="Положение по вертикали"><input type="range" min={0} max={100} value={vertical}
        disabled={height >= source.naturalHeight} onChange={event => setVertical(event.currentTarget.valueAsNumber)} /></Field>
      {error && <Notice error>{error}</Notice>}
      <Button onClick={apply}>Применить обрезку</Button>
    </div>
  </Modal>;
}

export function ProfileSettings({ account, onAccount, onAvatarChanged, onLogout, loggingOut, themeSettings }: {
  account: Account;
  onAccount: (account: Account) => void;
  onAvatarChanged: () => void;
  onLogout: () => void;
  loggingOut: boolean;
  themeSettings: ReactNode;
}) {
  const { profile, error } = useProfile(account.id);
  if (error) return <Notice error>{error}</Notice>;
  if (!profile) return <p role="status">Загружаем настройки…</p>;
  return <>
    <ProfileEditor account={account} profile={profile} onAccount={onAccount} onAvatarChanged={onAvatarChanged} />
    {themeSettings}
    <Button variant="secondary" busy={loggingOut} onClick={onLogout}>Выйти из аккаунта</Button>
  </>;
}

function ProfileEditor({ account, profile, onAccount, onAvatarChanged }: {
  account: Account; profile: Profile; onAccount: (account: Account) => void; onAvatarChanged: () => void;
}) {
  const avatarInput = useRef<HTMLInputElement>(null);
  const coverInput = useRef<HTMLInputElement>(null);
  const [name, setName] = useState(account.userName ?? '');
  const [bio, setBio] = useState(profile.bio ?? '');
  const [pending, setPending] = useState<Partial<Record<ImageKind, Preview>>>({});
  const [crop, setCrop] = useState<{ kind: ImageKind; source: HTMLImageElement }>();
  const [imageBusy, setImageBusy] = useState(false);
  const [imageError, setImageError] = useState('');
  const [imageStatus, setImageStatus] = useState('');
  const [accountBusy, setAccountBusy] = useState(false);
  const [accountError, setAccountError] = useState('');
  const [accountStatus, setAccountStatus] = useState('');
  const [revision, setRevision] = useState(0);

  async function choose(kind: ImageKind, file: File) {
    setImageBusy(true);
    setImageError('');
    setImageStatus('');
    try {
      setCrop({ kind, source: await readPreview(file) });
    } catch (failure) {
      setImageError((failure as Error).message);
    } finally { setImageBusy(false); }
  }

  async function applyImages() {
    setImageBusy(true);
    setImageError('');
    const applied: string[] = [];
    try {
      for (const kind of imageKinds) {
        const preview = pending[kind];
        if (!preview) continue;
        await request<void>(`/profile/media/${kind}`, { image: preview.image }, 'PUT');
        setPending(previous => ({ ...previous, [kind]: undefined }));
        setRevision(previous => previous + 1);
        if (kind === 'avatar') onAvatarChanged();
        applied.push(imageLabels[kind]);
      }
      setImageStatus('Изображения сохранены.');
    } catch (failure) {
      setImageStatus(applied.length ? `Сохранено: ${applied.join(', ')}.` : '');
      setImageError(failure instanceof ApiError && failure.status === 413
        ? 'Изображение должно весить не больше 5 МиБ.' : (failure as Error).message);
    } finally { setImageBusy(false); }
  }

  async function saveAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const fields = new FormData(form);
    setAccountBusy(true);
    setAccountError('');
    setAccountStatus('');
    try {
      const saved = await request<Account>('/profile/settings', {
        userName: name, bio, email: fields.get('email'),
        currentPassword: fields.get('currentPassword'), newPassword: fields.get('newPassword'),
      }, 'PUT');
      onAccount(saved);
      setName(saved.userName ?? '');
      setBio(bio.trim());
      for (const field of form.querySelectorAll<HTMLInputElement>('input[type="password"]')) field.value = '';
      setAccountStatus('Настройки аккаунта сохранены.');
    } catch (failure) {
      setAccountError((failure as Error).message);
    } finally { setAccountBusy(false); }
  }

  return <>
    <ProfileMedia avatar={pending.avatar?.dataUrl ?? `${mediaUrl(account.id, 'avatar')}?v=${revision}`}
      cover={pending.background?.dataUrl ?? `${mediaUrl(account.id, 'background')}?v=${revision}`}
      editAvatar={<button type="button" className="profile-edit-avatar icon-button" disabled={imageBusy}
        title="Изменить аватар" aria-label="Изменить аватар" onClick={() => avatarInput.current?.click()}><Pencil size={18} /></button>}
      editCover={<button type="button" className="profile-edit-cover button secondary" disabled={imageBusy}
        onClick={() => coverInput.current?.click()}><Pencil size={16} /> Изменить обложку</button>}>
      <Identity name={name.trim() || 'Игрок'} bio={bio.trim()} registeredAt={profile.registeredAt} isAdmin={profile.isAdmin} />
    </ProfileMedia>
    {crop && <ImageCrop kind={crop.kind} source={crop.source} onClose={() => setCrop(undefined)}
      onApply={preview => {
        setPending(previous => ({ ...previous, [crop.kind]: preview }));
        setCrop(undefined);
      }} />}
    {imageKinds.map(kind => <input key={kind} hidden type="file" ref={kind === 'avatar' ? avatarInput : coverInput}
      accept="image/jpeg,image/png,image/webp" disabled={imageBusy} onChange={event => {
        const file = event.currentTarget.files?.[0];
        event.currentTarget.value = '';
        if (file) void choose(kind, file);
      }} />)}
    {(pending.avatar || pending.background || imageBusy || imageError || imageStatus) &&
      <div className="profile-media-feedback" aria-busy={imageBusy}>
        {(pending.avatar || pending.background) && <div className="profile-preview-actions">
          <span className="muted">Предпросмотр фото</span>
          <div className="inline-actions">
            <Button disabled={imageBusy} onClick={() => void applyImages()}>Принять изменения</Button>
            <Button variant="secondary" disabled={imageBusy} onClick={() => {
              setPending({}); setImageError(''); setImageStatus('');
            }}>Сбросить</Button>
          </div>
        </div>}
        {imageError && <Notice error>{imageError}</Notice>}
        <small role="status">{imageBusy ? 'Обрабатываем изображения…' : imageStatus}</small>
      </div>}
    <section className="panel profile-section">
      <form className="form-stack" onSubmit={saveAccount}>
        <div className="profile-settings-grid">
          <fieldset className="profile-settings-group">
            <legend><User size={18} /> Публичный профиль</legend>
            <Field label="Никнейм"><input required autoComplete="username" maxLength={256} disabled={accountBusy}
            value={name} onChange={event => setName(event.target.value)} /></Field>
            <Field label="Описание" hint="Видно по ссылке на профиль · до 500 символов">
            <textarea rows={3} maxLength={500} disabled={accountBusy} value={bio} onChange={event => setBio(event.target.value)} />
            </Field>
          </fieldset>
          <fieldset className="profile-settings-group">
            <legend><LockKeyhole size={18} /> Вход и безопасность</legend>
            <Field label="Почта"><input required name="email" type="email" autoComplete="email"
            maxLength={256} disabled={accountBusy} defaultValue={account.email ?? ''} /></Field>
            <Field label="Текущий пароль" hint="Нужен для смены почты или пароля">
            <input name="currentPassword" type="password" autoComplete="current-password" maxLength={1024} disabled={accountBusy} />
            </Field>
            <Field label="Новый пароль"><input name="newPassword" type="password" autoComplete="new-password"
            minLength={10} maxLength={1024} disabled={accountBusy} /></Field>
          </fieldset>
        </div>
        <div><Button type="submit" busy={accountBusy}>Сохранить настройки</Button></div>
        {accountError && <Notice error>{accountError}</Notice>}
        <small role="status">{accountStatus}</small>
      </form>
    </section>
  </>;
}
