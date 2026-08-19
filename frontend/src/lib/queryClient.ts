import { QueryClient } from '@tanstack/react-query';
import type { AxiosError } from 'axios';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        // Don't retry 401 errors — the interceptor handles token refresh
        if ((error as AxiosError)?.response?.status === 401) return false;
        return failureCount < 2;
      },
    },
    mutations: {
      retry: false,
    },
  },
});
