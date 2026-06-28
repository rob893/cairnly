interface CairnMarkProps {
  /** Classes controlling the rendered size of the logo image. */
  className?: string;
}

/**
 * The Cairnly brand mark: the stacked-stone cairn logo. Rendered from the shared
 * logo image (the same asset used for the favicon) so branding stays consistent.
 * Decorative by default — pair it with the visible "Cairnly" wordmark for labeling.
 */
export function CairnMark({ className }: CairnMarkProps) {
  return (
    <img
      src={`${import.meta.env.BASE_URL}logo.png`}
      alt=""
      aria-hidden="true"
      className={className}
    />
  );
}
