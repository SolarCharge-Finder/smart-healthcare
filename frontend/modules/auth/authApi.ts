import apiClient from "../../shared/apiClient";
import { LoginResponse, RegisterRequest } from "./authTypes";

export const loginApi = async (
  email: string,
  password: string
): Promise<LoginResponse> => {
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

export const verifyApi = async (token: string): Promise<any> => {
  const res = await apiClient.post("/auth/verify", { token });
  return res.data;
};