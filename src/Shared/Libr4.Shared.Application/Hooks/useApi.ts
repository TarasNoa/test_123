import { createSignal, createEffect, Accessor } from 'solid-js';
import { apiClient } from '../ApiClient';

export interface UseApiOptions {
  onSuccess?: (data: any) => void;
  onError?: (error: any) => void;
  skipInitialCall?: boolean;
}

export function useApi<T>(
  url: Accessor<string>,
  options: UseApiOptions = {}
) {
  const [data, setData] = createSignal<T | null>(null);
  const [loading, setLoading] = createSignal(false);
  const [error, setError] = createSignal<any>(null);

  createEffect(async () => {
    if (options.skipInitialCall) return;

    setLoading(true);
    setError(null);

    try {
      const response = await apiClient.get<T>(url());
      setData(response.data as T);
      options.onSuccess?.(response.data);
    } catch (err) {
      setError(err);
      options.onError?.(err);
    } finally {
      setLoading(false);
    }
  });

  const refetch = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await apiClient.get<T>(url());
      setData(response.data as T);
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  };

  return { data, loading, error, refetch };
}