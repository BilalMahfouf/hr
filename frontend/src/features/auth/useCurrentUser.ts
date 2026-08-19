import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api/api';
import type { CurrentUser } from './types';

export function useCurrentUser() {
  return useQuery({
    queryKey: ['auth-me'],
    queryFn: async () => {
      const response = await api.get<CurrentUser>('/auth/me');
      if (response.status !== 200) {
        throw new Error('Failed to fetch current user');
      }
      console.log('Fetched current user data from API:', response.data);

      const data = response.data;
       return data;
    },
    staleTime: 60000,
    retry: false,
  });
}
