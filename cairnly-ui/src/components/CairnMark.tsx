interface CairnMarkProps {
  /** Classes controlling size and color (uses `currentColor`). */
  className?: string;
}

/**
 * The Cairnly brand mark: a stack of balanced stones (a cairn), drawn with
 * `currentColor` so it inherits the surrounding text color. Largest stone sits
 * at the base, tapering upward.
 */
export function CairnMark({ className }: CairnMarkProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <ellipse cx="12" cy="19.2" rx="7.6" ry="2.6" fill="currentColor" opacity="0.95" />
      <ellipse cx="12" cy="13.6" rx="5.7" ry="2.3" fill="currentColor" opacity="0.8" />
      <ellipse cx="12" cy="8.7" rx="4" ry="1.95" fill="currentColor" opacity="0.65" />
      <ellipse cx="12" cy="4.6" rx="2.4" ry="1.45" fill="currentColor" opacity="0.5" />
    </svg>
  );
}
