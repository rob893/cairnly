// User / account types.

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
