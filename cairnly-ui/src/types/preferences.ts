import type { ThemeMode } from '../constants/theme';

// User preferences types.

export interface ThemePreferences {
  mode: ThemeMode;
  accent: string;
}

export interface UserPreferences {
  userId: number;
  theme: ThemePreferences;
}

export interface UpdateUserPreferencesRequest {
  theme: ThemePreferences;
}
