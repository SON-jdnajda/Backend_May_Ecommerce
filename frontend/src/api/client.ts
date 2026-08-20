import type { InternalAxiosRequestConfig } from "axios";
import axios from "axios";

const baseURL = import.meta.env.VITE_API_BASE_URL;

if(!baseURL) {
     throw new Error('VITE_API_BASE_URL is not set. Check frontend/.env');
}

let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
     accessToken = token;
}

const client = axios.create({
     baseURL,
     withCredentials: true,
     timeout: 10_000,
});

client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
     if(accessToken) {
          config.headers.Authorization = `Bearer ${accessToken}`;
     }
     return config;
});

export default client;