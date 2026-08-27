import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import type { EmployeeGroupResponse } from "./employee-group-api";
import EmployeeGroupsDataTable from "./employee-groups-data-table";

export default function EmployeeGroupsPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const handleView = (group: EmployeeGroupResponse) => {
    navigate(`/employee-groups/${group.id}`);
  };

  const handleNew = () => {
    navigate("/employee-groups/new");
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6 flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.employeeGroups.title)}
          </h1>
          <p className="text-slate-500">{t(i18nKeyContainer.employeeGroups.description)}</p>
        </div>

        <button
          type="button"
          onClick={handleNew}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 cursor-pointer"
        >
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          {t(i18nKeyContainer.employeeGroups.addTitle)}
        </button>
      </div>

      <EmployeeGroupsDataTable onView={handleView} />
    </div>
  );
}