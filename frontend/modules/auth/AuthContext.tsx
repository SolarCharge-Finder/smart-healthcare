'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import { LoginResponse } from './types/auth';
import { authStorage } from './infra/authStorage';

type AuthContextType = {
  user: LoginResponse | null;
  login: (data: LoginResponse) => void;
  logout: () => void;

  setUser: (data: Partial<LoginResponse>) => void;
};

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUserState] = useState<LoginResponse | null>(null);

  useEffect(() => {
    const storedUser = authStorage.getUser();
    if (storedUser) setUserState(storedUser);
  }, []);

  const login = (data: LoginResponse) => {
    authStorage.setAuth(data);
    setUserState(data);
  };

  const setUser = (updatedFields: Partial<LoginResponse>) => {
    setUserState((prev) => {
      if (!prev) return prev;

      const updated = {
        ...prev, // keep token + existing fields
        ...updatedFields, // overwrite changed fields
      };

      authStorage.setAuth(updated); // persist
      return updated;
    });
  };

  const logout = () => {
    authStorage.clear();
    setUserState(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, setUser }}>{children}</AuthContext.Provider>
  );
};

export const useAuthContext = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('AuthContext not found');
  return context;
};
