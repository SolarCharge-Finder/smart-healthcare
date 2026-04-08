"use client";

import { loginApi, registerApi, verifyApi } from "./authApi";
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

  const verify = async (token: string) => {
    const data = await verifyApi(token);
    return data;
  }

  const logout = () => {
    authStorage.clear();
  };

  return {
    login,
    register,
    verify,
    logout,
  };
};