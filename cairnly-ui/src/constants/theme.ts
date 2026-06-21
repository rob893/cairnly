/**
 * Theme constants: color-scheme modes and selectable accent-color presets.
 *
 * Accent presets only override `--accent`, `--accent-foreground` and `--focus`; the rest of the
 * HeroUI palette (defined in `index.css`) is shared across accents and both color schemes.
 */

export type ThemeMode = 'system' | 'light' | 'dark';

export interface AccentPreset {
  id: string;
  label: string;
  /** The accent color (used for --accent and --focus). */
  accent: string;
  /** A contrasting foreground for content placed on the accent color. */
  accentForeground: string;
}

export const DEFAULT_MODE: ThemeMode = 'dark';
export const DEFAULT_ACCENT_ID = 'green';

/**
 * Selectable accent swatches. The default (`green`) matches the base theme in `index.css`.
 * Each accent shares lightness/chroma and varies hue for a consistent feel.
 */
export const ACCENT_PRESETS: readonly AccentPreset[] = [
  { id: 'green', label: 'Green', accent: 'oklch(76.97% 0.2124 148.67)', accentForeground: 'oklch(15% 0.03 148.67)' },
  { id: 'teal', label: 'Teal', accent: 'oklch(76.97% 0.14 190)', accentForeground: 'oklch(15% 0.03 190)' },
  { id: 'blue', label: 'Blue', accent: 'oklch(70% 0.16 250)', accentForeground: 'oklch(98% 0.01 250)' },
  { id: 'indigo', label: 'Indigo', accent: 'oklch(64% 0.18 280)', accentForeground: 'oklch(98% 0.01 280)' },
  { id: 'purple', label: 'Purple', accent: 'oklch(66% 0.2 300)', accentForeground: 'oklch(98% 0.01 300)' },
  { id: 'pink', label: 'Pink', accent: 'oklch(72% 0.2 350)', accentForeground: 'oklch(15% 0.03 350)' },
  { id: 'red', label: 'Red', accent: 'oklch(66% 0.22 25)', accentForeground: 'oklch(98% 0.01 25)' },
  { id: 'orange', label: 'Orange', accent: 'oklch(76% 0.16 60)', accentForeground: 'oklch(15% 0.03 60)' }
] as const;

/**
 * Resolves an accent preset by id, falling back to the default green accent.
 *
 * @param id The accent preset id.
 * @returns The matching preset, or the default.
 */
export function getAccentPreset(id: string): AccentPreset {
  return ACCENT_PRESETS.find(preset => preset.id === id) ?? ACCENT_PRESETS[0];
}
