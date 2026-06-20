import type { ThemeMode } from '../constants/theme';

// Generic pagination types

export interface PageInfo {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  startCursor?: string;
  endCursor?: string;
  totalCount?: number;
}

export interface Edge<T> {
  node: T;
  cursor: string;
}

export interface CursorPaginatedResponse<T> {
  nodes?: T[];
  edges?: Edge<T>[];
  pageInfo: PageInfo;
}

export interface CursorPaginationQueryParameters {
  first?: number;
  after?: string;
  last?: number;
  before?: string;
}

// User / account types

export type LinkedAccountType = 'Google' | 'GitHub';

export interface LinkedAccount {
  id: string;
  linkedAccountType: LinkedAccountType;
  userId: number;
}

export interface UserDetails {
  id: number;
  userName: string;
  email: string;
  emailConfirmed: boolean;
  created: string;
  roles: string[];
  linkedAccounts: LinkedAccount[];
  lastLogin?: string | null;
  lastPasswordChange: string;
  lastEmailChange: string;
  lastUsernameChange: string;
  lastEmailConfirmationSent?: string | null;
}

export interface UpdateUsernameRequest {
  newUsername: string;
}

export interface UpdatePasswordRequest {
  oldPassword: string;
  newPassword: string;
}

// Preferences types

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
