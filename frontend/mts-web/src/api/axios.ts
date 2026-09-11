// src/api/axios.ts

import axios from 'axios';
const downloadUrls = {
  android: "https://play.google.com/store/apps/details?id=com.DreamCultivationGame.NghchThinSkyBreaker",
  ios: "https://apps.apple.com/vn/app/ngh%E1%BB%8Bch-thi%C3%AAn-sky-breaker/id6797402488?l=vi",
}
const hostnameMap = {
  'localhost': {
    download: downloadUrls.android,
    api: 'http://localhost:9481'
  },
  '127.0.0.1': {
    download: downloadUrls.android,
    api: 'http://127.0.0.1:9481'
  },
  'play.nghichthien.online': {
    download: downloadUrls.android,
    api: 'https://api.nghichthien.online'
  },
  'ios.nghichthien.online': {
    download: downloadUrls.ios,
    api: 'https://api-ios.nghichthien.online'
  },
}
const getDownloadUrl = () => {
  const downloadUrl = hostnameMap[window.location.hostname as keyof typeof hostnameMap].download
  return downloadUrl
}
const createApiBaseUrl = () => {
  const apiUrl = hostnameMap[window.location.hostname as keyof typeof hostnameMap].api
  return apiUrl
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

export {
  api,
  getDownloadUrl,
  downloadUrls,
}
