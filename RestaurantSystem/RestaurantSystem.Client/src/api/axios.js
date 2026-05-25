import axios from 'axios';
import { toast } from 'react-toastify';

const api = axios.create({
  baseURL: 'http://localhost:5137/api',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response && error.response.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const token = localStorage.getItem('token');
        const refreshToken = localStorage.getItem('refreshToken');

        const response = await axios.post('http://localhost:5137/api/account/refresh', {
          token,
          refreshToken
        });

        if (response.status === 200) {
          const { token: newToken, refreshToken: newRefreshToken } = response.data;
          localStorage.setItem('token', newToken);
          localStorage.setItem('refreshToken', newRefreshToken);

          // Update user in context/localStorage if needed
          const userStr = localStorage.getItem('user');
          if (userStr) {
            const user = JSON.parse(userStr);
            user.token = newToken;
            user.refreshToken = newRefreshToken;
            localStorage.setItem('user', JSON.stringify(user));
          }

          api.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
          return api(originalRequest);
        }
      } catch (refreshError) {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.location.href = '/login';
      }
    }

    if (error.response) {
      if (error.response.status === 404) {
        toast.error('Resource not found (404)');
      } else if (error.response.status >= 500) {
        toast.error('Server error. Please try again later.');
      } else if (error.response.data && typeof error.response.data === 'string') {
        toast.error(error.response.data);
      }
    } else if (error.request) {
      toast.error('Network error. Please check your connection.');
    }

    return Promise.reject(error);
  }
);

export default api;
