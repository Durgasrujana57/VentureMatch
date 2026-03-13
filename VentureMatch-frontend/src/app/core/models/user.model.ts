export interface User {
  id: string;
  email: string;
  fullName: string;
  headline: string;
  bio: string;
  location: string;
  avatarUrl?: string;
  skills: string[];
  interests: string[];
  availability: string;
  lookingFor: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  headline?: string;
  location?: string;
  skills?: string[];
  interests?: string[];
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  fullName: string;
  expiry: string;
}