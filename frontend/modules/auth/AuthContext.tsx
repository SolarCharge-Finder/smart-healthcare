"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { LoginResponse } from "./authTypes";
import { authStorage } from "./authStorage";

type AuthContextType = {
  user: LoginResponse | null;
  login: (data: LoginResponse) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<LoginResponse | null>(null);

  useEffect(() => {
    const storedUser = authStorage.getUser();
    if (storedUser) setUser(storedUser);
  }, []);

  const login = (data: LoginResponse) => {
    authStorage.setAuth(data);
    setUser(data); // update state with user data after login
  };

  const logout = () => {
    authStorage.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuthContext = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error("AuthContext not found");
  return context;
};