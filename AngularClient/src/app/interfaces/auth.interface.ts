export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  role: string;
  expiresAtUtc: string;
}

export interface ErrorResponse {
  message: string;
}

export interface Testimonial {
  id: string;
  authorUsername: string;
  message: string;
  createdAtUtc: string;
}
