import api from '@/lib/api/api';

export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface UpdateProfileRequest {
  userName: string;
  firstName: string;
  lastName: string;
}

export interface ChangeEmailRequest {
  email: string;
}

export const settingsApi = {
  getMe: async (): Promise<UserProfile> => {
    const response = await api.get<UserProfile>('/auth/me');
    if (response.status !== 200) {
      throw new Error('Failed to fetch user profile');
    }
    return response.data;
  },

  updateProfile: async (data: UpdateProfileRequest): Promise<void> => {
    const response = await api.put('/update-profile', data);
    if (response.status !== 204) {
      throw new Error('Failed to update profile');
    }
  },

  changeEmail: async (data: ChangeEmailRequest): Promise<void> => {
    const response = await api.patch('/change-email', data);
    if (response.status !== 204) {
      throw new Error('Failed to change email');
    }
  },
};
