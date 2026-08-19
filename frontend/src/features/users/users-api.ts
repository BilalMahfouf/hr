import type { PagedList, TableRequest } from "@/components/tables";
import api from "@/lib/api/api";
import { getTableRequsestParams } from "@/lib/utils";
import i18n from "@/lib/i18n";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export type UserRecord = {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdOnUtc: string;
};

const usersApi = {
  getAllUsers: async (request: TableRequest): Promise<PagedList<UserRecord>> => {
    const params = getTableRequsestParams(request);
    const result = await api.get<PagedList<UserRecord>>("/users", { params });

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.user.fetchUsers));
    }

    return result.data;
  },

  getUserById: async (userId: string): Promise<UserRecord> => {
    const result = await api.get<UserRecord>(`/users/${userId}`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.user.fetchUser));
    }

    return result.data;
  },
};

export default usersApi;
