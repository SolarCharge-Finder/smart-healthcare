import axios from "axios";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL;

const api = axios.create({
  baseURL: apiBaseUrl ?? "",
  headers: {
    "Content-Type": "application/json"
  }
});

api.interceptors.request.use((config) => {
  if (!apiBaseUrl) {
    return Promise.reject(
      new Error("NEXT_PUBLIC_API_URL is required for frontend API calls")
    );
  }

  return config;
});

const rawApiKey = process.env.NEXT_PUBLIC_API_KEY;
const apiKey =
  rawApiKey && rawApiKey !== "change-me"
    ? rawApiKey
    : "dev-key";

if (apiKey) {
  api.defaults.headers.common["X-API-KEY"] = apiKey;
}

export const telemedicineApi = axios.create({
  baseURL: process.env.NEXT_PUBLIC_TELEMEDICINE_API_URL ?? "http://localhost:5005",
  headers: {
    "Content-Type": "application/json"
  }
});

export const paymentApi = axios.create({
  baseURL: process.env.NEXT_PUBLIC_PAYMENT_API_URL || "http://localhost:8083",
  headers: {
    "Content-Type": "application/json"
  }
});

export const notificationApi = axios.create({
  baseURL: process.env.NEXT_PUBLIC_NOTIFICATION_API_URL || "http://localhost:8082",
  headers: {
    "Content-Type": "application/json"
  }
});

export const doctorApi = axios.create({
  baseURL: process.env.NEXT_PUBLIC_DOCTOR_API_URL || "http://localhost:8083",
  headers: {
    "Content-Type": "application/json"
  }
});

if (apiKey) {
  paymentApi.defaults.headers.common["X-API-KEY"] = apiKey;
  telemedicineApi.defaults.headers.common["X-API-KEY"] = apiKey;
  notificationApi.defaults.headers.common["X-API-KEY"] = apiKey;
}

export const paymentPortalBaseUrl =
  process.env.NEXT_PUBLIC_PAYMENT_PORTAL_URL || "/payment";

export default api;
