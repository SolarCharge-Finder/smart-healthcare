import axios from "axios";

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
  headers: {
    "Content-Type": "application/json"
  }
});

const apiKey = process.env.NEXT_PUBLIC_API_KEY;
if (apiKey) {
  api.defaults.headers.common["X-API-KEY"] = apiKey;
}

export default api;
