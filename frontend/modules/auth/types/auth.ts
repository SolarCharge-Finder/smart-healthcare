export interface LoginResponse {
  token: string;
  name: string;
  email: string;
  role: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface CurrentUserResponse {
  id: string;
  name: string;
  email: string;
  role: string;
}
