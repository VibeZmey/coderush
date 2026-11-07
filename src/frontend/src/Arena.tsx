import { lazy, Suspense, useEffect, useRef } from 'react';
import { ArrowRight, ArrowUpRight, Check, Clock3, Code2, Target, Terminal, Trophy, Zap } from './icons';
import { Button } from './components';

const HeroCodeEditor = lazy(() => import('./HeroCodeEditor'));

const questions = [
  ['С чего начать?', 'Открой тренировку, выбери сложность, количество задач и время. Для запуска понадобится аккаунт.'],
  ['Нужно устанавливать редактор?', 'Нет. Условие, редактор и результаты проверки доступны прямо в браузере.'],
  ['Какие языки доступны?', 'Платформа поддерживает Python, C++, C#, JavaScript, Go и C. Набор языков конкретной задачи зависит от её настроек.'],
  ['Можно выбрать время?', 'Да. В тренировке ты задаёшь время на каждую задачу. У каждой задачи свой таймер.'],
  ['Что произойдёт, если закрыть вкладку?', 'Таймер продолжит идти. Вернуться к созданной тренировке можно из списка тренировок.'],
  ['Уже можно играть против другого участника?', 'Да. Открой «Дуэли» и начни поиск рейтингового соперника или пригласи друга по ссылке.'],
];

export default function Arena({ name, matches, runs, start }: { name?: string; matches: number; runs: number; start: () => void }) {
  const root = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const observer = new IntersectionObserver(entries => entries.forEach(entry => {
      if (entry.isIntersecting) { entry.target.classList.add('revealed'); observer.unobserve(entry.target); }
    }), { threshold: .15 });
    root.current?.querySelectorAll('[data-reveal]').forEach(element => { element.classList.add('reveal-ready'); observer.observe(element); });
    const words = new IntersectionObserver(entries => entries.forEach(entry => {
      if (entry.isIntersecting) { entry.target.classList.add('word-visible'); words.unobserve(entry.target); }
    }), { rootMargin: '0px 0px -18% 0px' });
    root.current?.querySelectorAll('.tagline-word').forEach(element => words.observe(element));
    return () => { observer.disconnect(); words.disconnect(); };
  }, []);
  return <div className="arena-home" ref={root}>
    <section className="arena-hero">
      <div className="arena-intro">
        <div className="arena-kicker caption-3"><span className="status-square" /> Код. Время. Характер.</div>
        <h1 className="heading-1">Твой код.<br />Твой следующий<br /><span className="accent-word">уровень.</span></h1>
        <p className="body-3">Короткие матчи по программированию.<br />Решай задачи, держи темп и становись сильнее с каждой попыткой.</p>
        <Button onClick={start}>Начать тренировку <ArrowUpRight size={20} /></Button>
        <span className="hero-footnote caption-2"><span className="green-dot" /> Тренировки и дуэли 1 на 1</span>
      </div>
      <div className="arena-visual" role="group" aria-label="Интерактивный пример кода Python">
        <div className="arena-orbit orbit-a" /><div className="arena-orbit orbit-b" />
        <span className="visual-index caption-3">CODE / COMPILE / REPEAT</span>
        <div className="code-window">
          <div className="code-window-head"><span><i /><i /><i /></span><span className="caption-4">warmup.py</span><Code2 size={18} /></div>
          <div className="hero-code-editor"><Suspense fallback={<pre className="hero-code-fallback">{'# Твой следующий ход\ndef solve(numbers):\n    result = 0\n    for n in numbers:\n        result += n\n    return result'}</pre>}><HeroCodeEditor /></Suspense></div>
          <div className="code-window-bottom caption-4"><span><span className="green-dot" /> Python</span><span>Попробуй изменить код <Terminal size={14} /></span></div>
        </div>
        <div className="arena-timer"><Clock3 size={20} /><div><span className="caption-4">Твой раунд</span><strong>05:00</strong></div><span className="timer-track"><i /></span></div>
        <div className="passed-stamp"><Check size={20} /><span className="title-4">Решение принято</span><span className="caption-4">Иллюстрация</span></div>
        <span className="visual-star star-one">✳</span><span className="visual-star star-two">+</span>
      </div>
    </section>
    <div className="language-rail"><span className="caption-3">Твой язык. Твои правила.</span><div>{['Python', 'C++', 'JavaScript', 'C#', 'Go', 'C'].map((language, i) => <span className="title-3" key={language}><i>{String(i + 1).padStart(2, '0')}</i>{language}</span>)}</div></div>
    <section className="arena-modes" data-reveal>
      <div className="section-heading"><div><span className="eyebrow">01 / Разминка перед рывком</span><h2 className="heading-3">Включай режим практики.</h2></div><p className="body-6">{name ? `${name}, продолжим с того места, где остановились.` : 'Меньше подготовки. Больше настоящего кода.'}</p></div>
      <div className="mode-grid">
        <button className="mode-card mode-featured" onClick={start}><div className="mode-card-top"><span className="mode-icon"><Target size={28} /></span><span className="badge green">Доступно сейчас</span></div><div><span className="caption-3">SOLO / ТРЕНИРОВКА</span><h3 className="heading-4">Обгони себя вчерашнего.</h3><p className="body-6">Выбери сложность и время. Проверь решение на тестах. Попробуй ещё раз, уже с новым опытом.</p></div><div className="mode-card-foot"><span className="caption-2"><Clock3 size={16} /> Свой таймер <span>·</span> Свой уровень</span><span className="mode-arrow"><ArrowUpRight size={24} /></span></div></button>
        <div className="mode-secondary"><a href="#problems" className="mode-card compact"><span className="mode-icon"><Code2 size={24} /></span><div><h3 className="heading-6">Управление задачами</h3><p className="body-6">Условия и тесты. Для администраторов.</p></div><ArrowUpRight size={22} /></a><a href="#duels" className="mode-card compact"><span className="mode-icon green-icon"><Trophy size={24} /></span><div><h3 className="heading-6">Дуэли 1 на 1</h3><p className="body-6">Найди соперника и сыграй на рейтинг.</p></div><ArrowUpRight size={22} /></a><div className="session-pulse"><span className="caption-2">В этой сессии</span><span><b>{matches}</b> тренировок</span><span><b>{runs}</b> запусков</span></div></div>
      </div>
    </section>
    <section className="arena-manifesto"><span className="eyebrow">Не просто решать. Расти.</span><h2 className="heading-2">{'Каждая попытка делает тебя сильнее. Каждый раунд приближает к цели.'.split(' ').map((word, i) => <span className="tagline-word" key={i}>{word} </span>)}</h2><Zap size={64} weight="duotone" /></section>
    <section className="arena-steps" data-reveal><div className="section-heading"><div><span className="eyebrow">02 / От старта до решения</span><h2 className="heading-3">Поймай свой темп.</h2></div><a href="#training" className="text-button">К тренировке <ArrowRight size={18} /></a></div><div className="steps-grid">{[[Target, '01', 'Настрой раунд', 'Сложность, задачи и время. Ты выбираешь, каким будет твой вызов.'], [Code2, '02', 'Напиши решение', 'Открой условие, выбери язык и сосредоточься на главном.'], [Trophy, '03', 'Разбери результат', 'Проверь тесты и найди, что улучшить в следующей попытке.']].map(([Icon, index, title, description]) => { const StepIcon = Icon as typeof Target; return <article key={String(index)}><div className="step-top"><span className="caption-3">/{String(index)}</span><StepIcon size={24} /></div><h3 className="heading-6">{String(title)}</h3><p className="body-6">{String(description)}</p></article>; })}</div></section>
    <section className="arena-faq" data-reveal><div><span className="eyebrow">03 / Перед стартом</span><h2 className="heading-3">Есть вопросы?</h2><p className="body-6">Всё, что нужно для первой тренировки.</p></div><div>{questions.map(([question, answer]) => <details key={question}><summary className="title-3">{question}<span>+</span></summary><p className="body-6">{answer}</p></details>)}</div></section>
    <section className="arena-final" data-reveal><span className="caption-3">ТВОЙ СЛЕДУЮЩИЙ ХОД</span><h2 className="heading-2">Хватит откладывать.<br />Пора писать код.</h2><Button onClick={start}>Начать тренировку <ArrowUpRight size={20} /></Button><span className="caption-2">Параметры раунда можно выбрать до запуска.</span></section>
  </div>;
}

