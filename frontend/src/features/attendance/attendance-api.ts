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
};

export default attendanceApi;