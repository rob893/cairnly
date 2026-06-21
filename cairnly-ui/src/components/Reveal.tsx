import { useEffect, useRef, useState, type ReactNode } from 'react';

interface RevealProps {
  /** Content to reveal once it scrolls into view. */
  children: ReactNode;
  /** Extra classes applied to the wrapper element. */
  className?: string;
  /** Delay, in milliseconds, before the reveal transition runs. Useful for staggering. */
  delay?: number;
  /** Direction the content animates in from. Defaults to `up`. */
  from?: 'up' | 'down' | 'left' | 'right' | 'none';
}

const hiddenOffset: Record<NonNullable<RevealProps['from']>, string> = {
  up: 'translate-y-8',
  down: '-translate-y-8',
  left: 'translate-x-8',
  right: '-translate-x-8',
  none: ''
};

/**
 * Wraps content in a scroll-triggered reveal. The element fades and slides into
 * place the first time it enters the viewport, then stays visible. Honors
 * `prefers-reduced-motion` via Tailwind's `motion-reduce` variant.
 */
export function Reveal({ children, className = '', delay = 0, from = 'up' }: RevealProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    if (typeof IntersectionObserver === 'undefined') {
      setIsVisible(true);
      return;
    }

    const observer = new IntersectionObserver(
      entries => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setIsVisible(true);
            observer.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.15, rootMargin: '0px 0px -8% 0px' }
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  return (
    <div
      ref={ref}
      style={{ transitionDelay: `${delay}ms` }}
      className={[
        'transition-all duration-700 ease-out motion-reduce:transition-none motion-reduce:transform-none',
        isVisible ? 'opacity-100 translate-x-0 translate-y-0' : `opacity-0 ${hiddenOffset[from]}`,
        className
      ].join(' ')}
    >
      {children}
    </div>
  );
}
