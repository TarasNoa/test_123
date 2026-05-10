import axios, { AxiosInstance, AxiosError } from 'axios';

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: Record<string, string[]>;
}

export class ApiClient {
  private client: AxiosInstance;

  constructor(baseURL: string = process.env.REACT_APP_API_URL || 'http://localhost:5000') {
    this.client = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('accessToken');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    this.client.interceptors.response.use(
      (response) => response.data,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('accessToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  async get<T>(url: string): Promise<ApiResponse<T>> {
    return this.client.get<ApiResponse<T>>(url);
  }

  async post<T>(url: string, data: any): Promise<ApiResponse<T>> {
    return this.client.post<ApiResponse<T>>(url, data);
  }

  async put<T>(url: string, data: any): Promise<ApiResponse<T>> {
    return this.client.put<ApiResponse<T>>(url, data);
  }

  async delete<T>(url: string): Promise<ApiResponse<T>> {
    return this.client.delete<ApiResponse<T>>(url);
  }

  async patch<T>(url: string, data: any): Promise<ApiResponse<T>> {
    return this.client.patch<ApiResponse<T>>(url, data);
  }
}

export const apiClient = new ApiClient();