import type { CSSProperties } from 'react';
import './duels.css';

export type Rank = 'Intern' | 'Junior' | 'Middle' | 'Senior';
export type DuelResult = 'Win' | 'Loss' | 'Draw' | 'JudgeUnavailable';
export type DuelRating = {
  rating: number; rank: Rank; nextThreshold: number | null;
  lastResult: DuelResult | null; lastDelta: number | null;
};

const rankOrder: Rank[] = ['Intern', 'Junior', 'Middle', 'Senior'];
export const resultNames: Record<DuelResult, string> = {
  Win: 'Победа', Loss: 'Поражение', Draw: 'Ничья', JudgeUnavailable: 'Сбой проверки',
};
export const ratingDelta = (value: number) => value < 0 ? '−' + Math.abs(value) : '+' + value;
const ratingFormat = new Intl.NumberFormat('ru-RU');

export function DuelRankBadge({ rank, rating }: { rank: Rank; rating: number }) {
  return <span className="duel-rank-badge" data-rank={rank}>{rank} · {ratingFormat.format(rating)}</span>;
}

export function DuelRatingCard({ rating, compact = false, label = 'Рейтинг дуэлей' }: { rating: DuelRating; compact?: boolean; label?: string }) {
  const { rank, nextThreshold } = rating;
  const floor = nextThreshold === null ? 1600 : nextThreshold - 200;
  const progress = nextThreshold === null ? 100 : Math.max(0, (rating.rating - floor) / (nextThreshold - floor) * 100);
  const nextRank = rankOrder[rankOrder.indexOf(rank) + 1];
  return <section className="panel duel-rating" data-rank={rank} aria-label={label}>
    <div className="duel-rating-ring" style={{ '--rank-progress': progress + '%' } as CSSProperties} aria-hidden="true">
      <span>{rank.slice(0, 1)}</span>
    </div>
    <div className="duel-rating-content">
      <div className="eyebrow">{label}</div>
      <div className="duel-rating-line">
        <strong>{ratingFormat.format(rating.rating)}</strong>
        <span className="duel-rank-name">{rank}</span>
        {rating.lastResult && rating.lastDelta !== null
          ? <span className="duel-result" data-result={rating.lastResult} title="Последняя рейтинговая игра">
            {resultNames[rating.lastResult]} {ratingDelta(rating.lastDelta)}
          </span>
          : <span className="muted">Первый матч ещё не сыгран</span>}
      </div>
      {!compact && (nextThreshold === null ? <p className="muted duel-progress-label">Высший ранг</p> : <>
        <progress className="duel-progress" max={100} value={progress} aria-label={'Прогресс до ' + nextRank} />
        <div className="duel-progress-label"><span>{ratingFormat.format(floor)}</span>
          <strong>До {nextRank} — {ratingFormat.format(nextThreshold - rating.rating)} очков</strong>
          <span>{ratingFormat.format(nextThreshold)}</span></div>
      </>)}
    </div>
  </section>;
}
