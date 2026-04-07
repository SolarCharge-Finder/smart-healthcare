import apiClient from "../../shared/apiClient";
import { AuthResponse, RegisterRequest } from "./types";

export const loginApi = async (
  email: string,
  password: string
): Promise<AuthResponse> => {
  const res = await apiClient.post("/auth/login", {
    email,
    password,
  });

  return res.data;
};

export const registerApi = async (
  data: RegisterRequest
): Promise<any> => {
  const res = await apiClient.post("/auth/register", data);
  return res.data;
};