import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { Fingerprint } from "lucide-react";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import type { PunchRecord } from "./attendance-api";
import PunchesDataTable from "./punches-data-table";

export default function PunchesPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const handleView = (punch: PunchRecord) => {
    navigate(`/attendance/punches/${punch.punchId}`);
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
            <Fingerprint className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-slate-900">
              {t(i18nKeyContainer.attendance.punches.title)}
            </h1>
            <p className="text-slate-500">
              {t(i18nKeyContainer.attendance.punches.description)}
            </p>
          </div>
        </div>
      </div>

      <PunchesDataTable onView={handleView} />
    </div>
  );
}
