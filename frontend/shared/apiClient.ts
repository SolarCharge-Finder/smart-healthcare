// apiClient.ts for authentication-related API calls, with token handling (adeesha)

import axios from "axios";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL;

const apiClient = axios.create({
  baseURL: apiBaseUrl ?? "", 
  headers: {
    "Content-Type": "application/json"
  }
});

apiClient.interceptors.request.use((config) => {
  if (!apiBaseUrl) {
    return Promise.reject(
      new Error("NEXT_PUBLIC_API_URL is required for frontend API calls")
    );
  }

  return config;
});

apiClient.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("token");

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }

  return config;
});

export default apiClient;