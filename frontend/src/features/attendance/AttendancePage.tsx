import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import PunchesDataTable from "./punches-data-table";
import AttendanceRecordsDataTable from "./records-data-table";
import type { PunchRecord, AttendanceRecord } from "./attendance-api";

export default function AttendancePage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const handleViewPunch = (punch: PunchRecord) => {
    navigate(`/attendance/punches/${punch.punchId}`);
  };

  const handleViewRecord = (record: AttendanceRecord) => {
    navigate(`/attendance/records/${record.attendanceRecordId}`);
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.attendance.title)}
        </h1>
        <p className="text-slate-500">{t(i18nKeyContainer.attendance.description)}</p>
      </div>

      <Tabs defaultValue="punches" dir={isRtl ? "rtl" : "ltr"}>
        <TabsList className="mb-4">
          <TabsTrigger value="punches" className="cursor-pointer">
            {t(i18nKeyContainer.attendance.tabs.punches)}
          </TabsTrigger>
          <TabsTrigger value="records" className="cursor-pointer">
            {t(i18nKeyContainer.attendance.tabs.records)}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="punches">
          <PunchesDataTable onView={handleViewPunch} />
        </TabsContent>

        <TabsContent value="records">
          <AttendanceRecordsDataTable onView={handleViewRecord} />
        </TabsContent>
      </Tabs>
    </div>
  );
}