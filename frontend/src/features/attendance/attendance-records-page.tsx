import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import type { AttendanceRecord } from "./attendance-api";
import AttendanceRecordsDataTable from "./records-data-table";

export default function AttendanceRecordsPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const handleView = (record: AttendanceRecord) => {
    navigate(`/attendance/records/${record.attendanceRecordId}`);
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.attendance.records.title)}
        </h1>
        <p className="text-slate-500">
          {t(i18nKeyContainer.attendance.records.description)}
        </p>
      </div>

      <AttendanceRecordsDataTable onView={handleView} />
    </div>
  );
}
