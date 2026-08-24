import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import type { EmployeeListItem } from "./employees-api";
import EmployeesDataTable from "./employees-data-table";

export default function EmployeesPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const handleView = (employee: EmployeeListItem) => {
    navigate(`/employees/${employee.matricule}`);
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.employees.title)}
        </h1>
        <p className="text-slate-500">
          {t(i18nKeyContainer.employees.description)}
        </p>
      </div>

      <EmployeesDataTable onView={handleView} />
    </div>
  );
}
