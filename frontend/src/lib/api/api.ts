
import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 10000,
  withCredentials: true, // ensures cookies are sent
  headers: {
    'Content-Type': 'application/json',
  },
});

export default api;
