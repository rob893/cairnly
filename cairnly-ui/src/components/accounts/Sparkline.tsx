interface SparklineProps {
  /** Relative data points; the line is scaled to fit the view box. */
  points: number[];
  /** Stroke color (defaults to a muted theme token). */
  color?: string;
  className?: string;
}

/**
 * A tiny dependency-free line chart for inline account trends. Points are scaled
 * to fill the fixed view box; a flat series renders as a centered horizontal line.
 */
export function Sparkline({ points, color = 'var(--muted)', className }: SparklineProps) {
  const width = 64;
  const height = 24;
  const padding = 2;

  if (points.length === 0) {
    return null;
  }

  const max = Math.max(...points);
  const min = Math.min(...points);
  const range = max - min || 1;
  const step = points.length > 1 ? (width - padding * 2) / (points.length - 1) : 0;

  const d = points
    .map((value, index) => {
      const x = padding + index * step;
      const y = height - padding - ((value - min) / range) * (height - padding * 2);
      return `${index === 0 ? 'M' : 'L'}${x.toFixed(1)} ${y.toFixed(1)}`;
    })
    .join(' ');

  return (
    <svg
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <path d={d} stroke={color} strokeWidth={1.5} strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
