// src/api/axios.ts

import axios from 'axios';

const createApiBaseUrl = () => {
  const url = new URL(window.location.origin);
  const isIpAddress = /^\d{1,3}(\.\d{1,3}){3}$/.test(url.hostname) || url.hostname.includes(':');
  const isLocalHost =
    url.hostname === 'localhost' ||
    url.hostname === '127.0.0.1' ||
    url.hostname === '0.0.0.0' ||
    url.hostname === '::1' ||
    isIpAddress;

  if (isLocalHost) {
    url.port = '9481';
    return url.origin;
  }

  url.port = '';

  if (!url.hostname.startsWith('api.')) {
    const hostnameParts = url.hostname.split('.').filter(Boolean);

    if (hostnameParts.length > 2) {
      hostnameParts[0] = 'api';
      url.hostname = hostnameParts.join('.');
    } else {
      url.hostname = `api.${url.hostname}`;
    }
  }

  return url.origin;
};

const api = axios.create({
  baseURL: createApiBaseUrl(),
  headers: {
    'Content-Type': 'application/json'
  },
  validateStatus: (status) => status >= 200 && status < 400
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,

  (error) => {
    const status = error.response?.status;
    if (status === 400) {
      return error.response;
    }

    if (status === 401) {
      localStorage.removeItem('accessToken');
      window.location.href = '/auth';
    }

    return Promise.reject(error);
  }
);

export default api;
