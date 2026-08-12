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
  username: string;
  role: string;
  expiresAtUtc: string;
}

export interface MeResponse {
  username: string;
  role: string;
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

/** #45's public resume-poster directory entry - name only, never an email address. */
export interface ResumePoster {
  id: string;
  displayName: string;
}

export interface ContactPosterRequest {
  message: string;
  replyToEmail: string;
}
