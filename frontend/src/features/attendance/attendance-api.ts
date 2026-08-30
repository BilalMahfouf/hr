import type { PagedList, TableRequest } from "@/components/tables";
import api from "@/lib/api/api";
import { getTableRequsestParams } from "@/lib/utils";
import i18n from "@/lib/i18n";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export type PunchRecord = {
  punchId: string;
  machineId: string;
  machineIp: string | null;
  employeeId: string;
  employeeFullName: string | null;
  punchOccurredOnUtc: string;
  createdOnUtc: string;
};

export type AttendanceRecord = {
  attendanceRecordId: string;
  employeeId: string;
  employeeFullName: string;
  checkInAt: string;
  checkOutAt: string | null;
  workedTime: string;
  isAbsent: boolean;
};

export type PunchPollingSettings = {
  isEnabled: boolean;
  intervalMinutes: number;
  updatedAt: string;
};

export type UpdatePunchPollingSettingsRequest = {
  isEnabled: boolean;
  intervalMinutes: number;
};

export type PullNowResponse = {
  machineCount: number;
  punchCount: number;
};

const attendanceApi = {
  getAllPunches: async (request: TableRequest): Promise<PagedList<PunchRecord>> => {
    const params = getTableRequsestParams(request);
    const result = await api.get<PagedList<PunchRecord>>("/attendance/punches", { params });

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.fetchPunches));
    }

    return result.data;
  },

  getPunchById: async (punchId: string): Promise<PunchRecord> => {
    const result = await api.get<PunchRecord>(`/attendance/punches/${punchId}`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.fetchPunch));
    }

    return result.data;
  },

  getAllAttendanceRecords: async (request: TableRequest): Promise<PagedList<AttendanceRecord>> => {
    const params = getTableRequsestParams(request);
    const result = await api.get<PagedList<AttendanceRecord>>("/attendance/records", { params });

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.fetchRecords));
    }

    return result.data;
  },

  getAttendanceRecordById: async (attendanceRecordId: string): Promise<AttendanceRecord> => {
    const result = await api.get<AttendanceRecord>(`/attendance/records/${attendanceRecordId}`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.fetchRecord));
    }

    return result.data;
  },

  getPunchPollingSettings: async (): Promise<PunchPollingSettings> => {
    const result = await api.get<PunchPollingSettings>("/attendance/punch-polling");

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.fetchPollingSettings));
    }

    return result.data;
  },

  updatePunchPollingSettings: async (data: UpdatePunchPollingSettingsRequest): Promise<void> => {
    const result = await api.put("/attendance/punch-polling", data);

    if (result.status !== 204) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.updatePollingSettings));
    }
  },

  runPunchPollingNow: async (): Promise<PullNowResponse> => {
    const result = await api.post<PullNowResponse>("/attendance/punch-polling/run");

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.attendance.runPollingNow));
    }

    return result.data;
  },
};

export default attendanceApi;