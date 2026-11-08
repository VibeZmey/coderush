import Admin from './Admin';
import Problems from './Problems';
import type { Account } from './api';
import { ArrowLeft, Code2, Trophy } from './icons';
import './admin-panel.css';

const categories = [
  { id: 'duel', title: 'Дуэльные задачи', icon: Trophy },
  { id: 'practice', title: 'Задачи для решения', icon: Code2 },
] as const;

type Pool = typeof categories[number]['id'];

export function getAdminPool(): Pool {
  return new URLSearchParams(location.hash.split('?')[1]).get('pool') === 'practice' ? 'practice' : 'duel';
}

export default function AdminPanel({ pool, creating, ...shared }: {
  pool: Pool;
  creating: boolean;
  account: Account | null;
  authorized: () => boolean;
  notify: (message: string) => void;
}) {
  const category = categories.find(item => item.id === pool)!;
  const listHref = `#problems?pool=${pool}`;
  const isPractice = pool === 'practice';

  return <div className="admin-panel-layout">
    <nav className="admin-panel-nav panel" aria-label="Разделы админки">
      <span className="eyebrow">АДМИН-ПАНЕЛЬ</span>
      {categories.map(({ id, title, icon: Icon }) => <a key={id} href={`#problems?pool=${id}`}
        aria-current={id === pool ? 'page' : undefined}><Icon size={18} aria-hidden="true" />{title}</a>)}
    </nav>
    <div className="admin-panel-content" key={pool}>
      {creating
        ? <><a className="button secondary" href={listHref}><ArrowLeft size={16} />К списку задач</a><Admin {...shared} isPractice={isPractice} /></>
        : <Problems {...shared} isPractice={isPractice} title={category.title} createHref={`#admin?pool=${pool}`} />}
    </div>
  </div>;
}
