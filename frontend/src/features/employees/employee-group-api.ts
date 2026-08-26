import type { PagedList, TableRequest } from "@/components/tables";
import api from "@/lib/api/api";
import { getTableRequsestParams } from "@/lib/utils";
import i18n from "@/lib/i18n";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export type Guid = string;
export type IsoDate = string;
export type IsoDateTime = string;
export type TimeOnly = string;

export type EmployeeGroupResponse = {
  id: Guid;
  name: string;
  isSecurity: boolean;
  description: string | null;
  rotationStartDate: IsoDate;
  numberOfRotations: number;
  workSchedules: WorkScheduleResponse[];
  rotationEntries: RotationEntryResponse[];
  createdOnUtc: IsoDateTime;
};

export type WorkScheduleResponse = {
  id: Guid;
  employeeGroupId: Guid;
  shiftStartTime: TimeOnly;
  shiftEndTime: TimeOnly;
  breakStartTime: TimeOnly;
  breakEndTime: TimeOnly;
  endDayOffset: number;
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
  isActive: boolean;
  createdOnUtc: IsoDateTime;
};

export type RotationEntryResponse = {
  id: Guid;
  employeeGroupId: Guid;
  position: number;
  workScheduleId: Guid | null;
  status: "Work" | "Rest";
};

export type CreateEmployeeGroupRequest = {
  name: string;
  isSecurity: boolean;
  description: string | null;
  rotationStartDate: IsoDate;
  workSchedules: CreateWorkScheduleRequest[];
  rotationEntries: CreateRotationEntryRequest[];
};

export type CreateWorkScheduleRequest = {
  shiftStartTime: TimeOnly;
  shiftEndTime: TimeOnly;
  breakStartTime: TimeOnly;
  breakEndTime: TimeOnly;
  endDayOffset: number;
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
};

export type CreateRotationEntryRequest = {
  position: number;
  workScheduleId: Guid | null;
};

export type UpdateEmployeeGroupRequest = {
  name?: string;
  isSecurity?: boolean;
  description?: string | null;
};

export type ReplaceSchedulesAndRotationsRequest = {
  workSchedules: CreateWorkScheduleRequest[];
  rotationEntries: CreateRotationEntryRequest[];
};

export type CreateWorkRotationRequest = {
  position: number;
  workScheduleId: Guid;
};

export type CreateRestRotationRequest = {
  position: number;
};

export type UpdateRotationRequest = {
  newPosition?: number;
  workScheduleId?: Guid | null;
};

export type UpdateWorkScheduleRequest = {
  shiftStartTime: TimeOnly;
  shiftEndTime: TimeOnly;
  breakStartTime: TimeOnly;
  breakEndTime: TimeOnly;
  endDayOffset: number;
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
};

const employeeGroupApi = {
  getAllEmployeeGroups: async (
    request: TableRequest,
  ): Promise<PagedList<EmployeeGroupResponse>> => {
    const params = getTableRequsestParams(request);
    const result = await api.get<EmployeeGroupResponse[]>("/employee-groups", { params });

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.employeeGroup?.fetchEmployeeGroups ?? "errors.employee.fetchEmployees"));
    }

    const searched = searchEmployeeGroups(result.data, request.search);
    const sorted = sortEmployeeGroups(searched, request.sortColumn, request.sortOrder);
    return paginate(sorted, request);
  },

  getEmployeeGroupById: async (id: string): Promise<EmployeeGroupResponse> => {
    const result = await api.get<EmployeeGroupResponse>(`/employee-groups/${id}`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.employeeGroup?.fetchEmployeeGroup ?? "errors.employee.fetchEmployee"));
    }

    return result.data;
  },

  createEmployeeGroup: async (request: CreateEmployeeGroupRequest): Promise<EmployeeGroupResponse> => {
    const result = await api.post<EmployeeGroupResponse>("/employee-groups", request);

    if (result.status !== 201) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  updateEmployeeGroup: async (id: string, request: UpdateEmployeeGroupRequest): Promise<EmployeeGroupResponse> => {
    const result = await api.patch<EmployeeGroupResponse>(`/employee-groups/${id}`, request);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  replaceSchedulesAndRotations: async (
    id: string,
    request: ReplaceSchedulesAndRotationsRequest,
  ): Promise<EmployeeGroupResponse> => {
    const result = await api.put<EmployeeGroupResponse>(`/employee-groups/${id}/schedules-and-rotations`, request);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  deleteEmployeeGroup: async (id: string): Promise<void> => {
    const result = await api.delete(`/employee-groups/${id}`);

    if (result.status !== 204) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }
  },

  // Work Schedules (scoped to group)
  createWorkSchedule: async (
    groupId: string,
    request: CreateWorkScheduleRequest,
  ): Promise<WorkScheduleResponse> => {
    const result = await api.post<WorkScheduleResponse>(`/employee-groups/${groupId}/work-schedules`, request);

    if (result.status !== 201) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  getWorkScheduleById: async (groupId: string, scheduleId: string): Promise<WorkScheduleResponse> => {
    const result = await api.get<WorkScheduleResponse>(`/employee-groups/${groupId}/work-schedules/${scheduleId}`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  updateWorkSchedule: async (
    groupId: string,
    scheduleId: string,
    request: UpdateWorkScheduleRequest,
  ): Promise<WorkScheduleResponse> => {
    const result = await api.put<WorkScheduleResponse>(`/employee-groups/${groupId}/work-schedules/${scheduleId}`, request);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  deleteWorkSchedule: async (groupId: string, scheduleId: string): Promise<void> => {
    const result = await api.delete(`/employee-groups/${groupId}/work-schedules/${scheduleId}`);

    if (result.status !== 204) {
      if (result.status === 409) {
        throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.form?.scheduleInUse ?? "errors.employee.fetchEmployees"));
      }
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }
  },

  activateWorkSchedule: async (groupId: string, scheduleId: string): Promise<WorkScheduleResponse> => {
    const result = await api.post<WorkScheduleResponse>(`/employee-groups/${groupId}/work-schedules/${scheduleId}/activate`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  deactivateWorkSchedule: async (groupId: string, scheduleId: string): Promise<WorkScheduleResponse> => {
    const result = await api.post<WorkScheduleResponse>(`/employee-groups/${groupId}/work-schedules/${scheduleId}/deactivate`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  // Rotations (scoped to group)
  getRotations: async (groupId: string): Promise<RotationEntryResponse[]> => {
    const result = await api.get<RotationEntryResponse[]>(`/employee-groups/${groupId}/rotations`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  createWorkRotation: async (
    groupId: string,
    request: CreateWorkRotationRequest,
  ): Promise<RotationEntryResponse> => {
    const result = await api.post<RotationEntryResponse>(`/employee-groups/${groupId}/rotations/work`, request);

    if (result.status !== 201) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  createRestRotation: async (
    groupId: string,
    request: CreateRestRotationRequest,
  ): Promise<RotationEntryResponse> => {
    const result = await api.post<RotationEntryResponse>(`/employee-groups/${groupId}/rotations/rest`, request);

    if (result.status !== 201) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  updateRotation: async (
    groupId: string,
    position: number,
    request: UpdateRotationRequest,
  ): Promise<RotationEntryResponse> => {
    const result = await api.put<RotationEntryResponse>(`/employee-groups/${groupId}/rotations/${position}`, request);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }

    return result.data;
  },

  deleteRotation: async (groupId: string, position: number): Promise<void> => {
    const result = await api.delete(`/employee-groups/${groupId}/rotations/${position}`);

    if (result.status !== 204) {
      throw new Error(i18n.t(i18nKeyContainer.employeeGroups?.toast?.error ?? "errors.employee.fetchEmployees"));
    }
  },
};

const searchEmployeeGroups = (groups: EmployeeGroupResponse[], search?: string): EmployeeGroupResponse[] => {
  const term = search?.trim().toLowerCase() ?? "";
  if (!term) return groups;

  return groups.filter(
    (group) =>
      group.name.toLowerCase().includes(term) ||
      group.description?.toLowerCase().includes(term) ||
      group.id.toLowerCase().includes(term),
  );
};

const sortEmployeeGroups = (
  groups: EmployeeGroupResponse[],
  sortColumn?: string,
  sortOrder?: "asc" | "desc",
): EmployeeGroupResponse[] => {
  if (!sortColumn || !sortOrder) return groups;

  const sorted = [...groups].sort((a, b) => {
    const aValue = a[sortColumn as keyof EmployeeGroupResponse];
    const bValue = b[sortColumn as keyof EmployeeGroupResponse];
    if (aValue < bValue) return -1;
    if (aValue > bValue) return 1;
    return 0;
  });

  return sortOrder === "desc" ? sorted.reverse() : sorted;
};

const paginate = (groups: EmployeeGroupResponse[], request: TableRequest): PagedList<EmployeeGroupResponse> => {
  const { page, pageSize } = request;
  const start = (page - 1) * pageSize;
  const item = groups.slice(start, start + pageSize);

  return {
    item,
    totalCount: groups.length,
    pageSize,
    page,
    hasNextPage: start + pageSize < groups.length,
    hasPreviousPage: page > 1,
  };
};

export default employeeGroupApi;