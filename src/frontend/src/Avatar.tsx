import { useState, type ReactNode } from 'react';
import './avatar.css';

function AvatarImage({ src, fallback }: { src: string; fallback: ReactNode }) {
  const [failed, setFailed] = useState(false);
  return failed ? <>{fallback}</> : <img className="account-avatar-image" src={src} alt="" onError={() => setFailed(true)} />;
}

export default function Avatar({ name, matchId, revision, fallback }: {
  name: string;
  matchId?: string;
  revision?: number;
  fallback?: ReactNode;
}) {
  const src = matchId
    ? `/api/duels/${encodeURIComponent(matchId)}/opponent-avatar`
    : `/api/profile/media/avatar?v=${revision ?? 0}`;
  return <AvatarImage key={src} src={src} fallback={fallback ?? name.slice(0, 2).toUpperCase()} />;
}
