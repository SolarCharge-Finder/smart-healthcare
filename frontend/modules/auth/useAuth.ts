import { loginApi, registerApi } from "./api";

export const useAuth = () => {
  const login = async (email: string, password: string) => {
    const data = await loginApi(email, password);

    // store token
    localStorage.setItem("token", data.token);

    return data;
  };

  const register = async (name: string, email: string, password: string) => {
    const data = await registerApi({ name, email, password });

    return data;
  };

  const logout = () => {
    localStorage.removeItem("token");
  };

  return {
    login,
    register,
    logout,
  };
};