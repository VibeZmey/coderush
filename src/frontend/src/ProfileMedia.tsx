import { useState, type ReactNode } from 'react';
import { User } from './icons';
import './profile-media.css';

function Image({ src, fallback }: { src?: string; fallback?: ReactNode }) {
  const [failed, setFailed] = useState(false);
  return src && !failed ? <img src={src} alt="" onError={() => setFailed(true)} /> : <>{fallback}</>;
}

export default function ProfileMedia({ avatar, cover, children, actions, editAvatar, editCover }: {
  avatar?: string;
  cover?: string;
  children: ReactNode;
  actions?: ReactNode;
  editAvatar?: ReactNode;
  editCover?: ReactNode;
}) {
  return <section className="panel profile-hero" aria-label="Профиль">
    <div className="profile-cover"><Image key={cover} src={cover} />{editCover}</div>
    <div className="profile-hero-body">
      <div className="profile-portrait">
        <div className="profile-portrait-image">
          <Image key={avatar} src={avatar} fallback={<User size={32} />} />
        </div>
        {editAvatar}
      </div>
      <div className="profile-identity">{children}</div>
      {actions && <div className="profile-hero-actions">{actions}</div>}
    </div>
  </section>;
}
