export type Role = 'Administrator' | 'Commercial';

export interface UserSummary {
  id: string;
  displayName: string;
  role: Role;
  isActive: boolean;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresIn: number;
  user: UserSummary;
}
