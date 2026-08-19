import type { PagedList, TableRequest } from "@/components/tables";
import api from "@/lib/api/api";
import i18n from "@/lib/i18n";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export type MachineRecord = {
  machineId: string;
  machineNumber: number;
  ipAddress: string;
  port: number;
  isActive: boolean;
};

export type CreateMachineRequest = {
  ipAddress: string;
  machineNumber: number;
  port?: number;
};

export type UpdateMachineIpAddressRequest = {
  ipAddress: string;
};

interface CreateMachineResponse {
  machineId: string;
}

const searchMachines = (machines: MachineRecord[], search?: string): MachineRecord[] => {
  const term = search?.trim().toLowerCase() ?? "";
  if (!term) return machines;

  return machines.filter(
    (machine) =>
      String(machine.machineNumber).includes(term) ||
      machine.ipAddress.toLowerCase().includes(term),
  );
};

const sortMachines = (
  machines: MachineRecord[],
  sortColumn?: string,
  sortOrder?: "asc" | "desc",
): MachineRecord[] => {
  if (!sortColumn || !sortOrder) return machines;

  const sorted = [...machines].sort((a, b) => {
    const aValue = a[sortColumn as keyof MachineRecord];
    const bValue = b[sortColumn as keyof MachineRecord];
    if (aValue < bValue) return -1;
    if (aValue > bValue) return 1;
    return 0;
  });

  return sortOrder === "desc" ? sorted.reverse() : sorted;
};

const paginate = (machines: MachineRecord[], request: TableRequest): PagedList<MachineRecord> => {
  const { page, pageSize } = request;
  const start = (page - 1) * pageSize;
  const item = machines.slice(start, start + pageSize);

  return {
    item,
    totalCount: machines.length,
    pageSize,
    page,
    hasNextPage: start + pageSize < machines.length,
    hasPreviousPage: page > 1,
  };
};

const attendanceMachineApi = {
  getAllMachines: async (request: TableRequest): Promise<PagedList<MachineRecord>> => {
    const result = await api.get<MachineRecord[]>("/attendance/machines");

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.machine.fetchMachines));
    }

    const searched = searchMachines(result.data, request.search);
    const sorted = sortMachines(searched, request.sortColumn, request.sortOrder);
    return paginate(sorted, request);
  },

  createMachine: async (request: CreateMachineRequest): Promise<string> => {
    const result = await api.post<CreateMachineResponse>("/attendance/machines", request);

    if (result.status !== 201) {
      throw new Error(i18n.t(i18nKeyContainer.machines.genericError));
    }

    return result.data.machineId;
  },

  updateMachineIpAddress: async (
    machineId: string,
    request: UpdateMachineIpAddressRequest,
  ): Promise<void> => {
    const result = await api.put(`/attendance/machines/${machineId}`, request);

    if (result.status !== 204) {
      throw new Error(i18n.t(i18nKeyContainer.machines.genericError));
    }
  },

  activateMachine: async (machineId: string): Promise<void> => {
    const result = await api.patch(`/attendance/machines/${machineId}/activate`);

    if (result.status !== 204) {
      throw new Error(i18n.t(i18nKeyContainer.machines.genericError));
    }
  },

  deactivateMachine: async (machineId: string): Promise<void> => {
    const result = await api.patch(`/attendance/machines/${machineId}/deactivate`);

    if (result.status !== 204) {
      throw new Error(i18n.t(i18nKeyContainer.machines.genericError));
    }
  },
};

export default attendanceMachineApi;