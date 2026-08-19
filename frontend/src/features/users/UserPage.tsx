import { useState } from "react";
import { useTranslation } from "react-i18next";
import UsersDataTable from "./users-data-table";
import ViewUser from "./view-user";
import type { UserRecord } from "./users-api";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export default function UserPage() {
  const [viewUserOpen, setViewUserOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);

  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const handleView = (user: UserRecord) => {
    setSelectedUserId(user.id);
    setViewUserOpen(true);
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.staff.title)}
        </h1>
        <p className="text-slate-500">{t(i18nKeyContainer.staff.description)}</p>
      </div>

      <UsersDataTable onView={handleView} />

      {selectedUserId && (
        <ViewUser
          open={viewUserOpen}
          onClose={() => {
            setViewUserOpen(false);
            setSelectedUserId(null);
          }}
          userId={selectedUserId}
        />
      )}
    </div>
  );
}
