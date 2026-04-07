"use client";

import { loginApi, registerApi } from "./authApi";
import { authStorage } from "./authStorage";

export const useAuth = () => {
  const login = async (email: string, password: string) => {
    const data = await loginApi(email, password);

    // store data in localStorage
    authStorage.setAuth(data);

    return data;
  };

  const register = async (name: string, email: string, password: string) => {
    const data = await registerApi({ name, email, password });

    return data;
  };

  const logout = () => {
    authStorage.clear();
  };

  return {
    login,
    register,
    logout,
  };
};