import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Calendar, Hash, Mail, Shield, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { StatusBadge } from "@/components/tables";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import usersApi from "./users-api";

interface ViewUserProps {
  open: boolean;
  onClose: () => void;
  userId: string;
}

export default function ViewUser({ open, onClose, userId }: ViewUserProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const { data: user, isLoading } = useQuery({
    queryKey: ["user", userId],
    queryFn: () => usersApi.getUserById(userId),
    enabled: open && !!userId,
  });

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString(i18n.language, {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const statusLabel = user?.isActive
    ? t(i18nKeyContainer.staff.active)
    : t(i18nKeyContainer.staff.inactive);

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent
        className="max-w-2xl p-0 bg-white"
        dir={isRtl ? "rtl" : "ltr"}
        onInteractOutside={(e) => e.preventDefault()}
      >
        <div className="w-full">
          <div className="border-b border-slate-200 px-6 py-6">
            <div className="flex items-center gap-4">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                <User className="h-6 w-6 text-primary" />
              </div>
              <div className="flex-1">
                <h2 className="text-xl font-semibold text-slate-900">
                  {t(i18nKeyContainer.staff.title)}
                </h2>
                <p className="text-sm text-slate-500 mt-0.5">
                  {t(i18nKeyContainer.staff.description)}
                </p>
              </div>
            </div>
          </div>

          <div className="px-6 py-6">
            {isLoading ? (
              <div className="space-y-4">
                <div className="h-16 bg-slate-100 rounded animate-pulse" />
                <div className="h-16 bg-slate-100 rounded animate-pulse" />
                <div className="h-16 bg-slate-100 rounded animate-pulse" />
                <div className="h-16 bg-slate-100 rounded animate-pulse" />
              </div>
            ) : user ? (
              <div className="space-y-4">
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.user.userId)}
                  </label>
                  <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                    <Hash className="h-4 w-4 text-slate-400" />
                    <span className="text-slate-900 font-mono text-sm">{user.id}</span>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-slate-700">
                      {t(i18nKeyContainer.user.fullName)}
                    </label>
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                      <User className="h-4 w-4 text-slate-400" />
                      <span className="text-slate-900">{user.fullName}</span>
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-slate-700">
                      {t(i18nKeyContainer.settingsPage.profile.username)}
                    </label>
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                      <User className="h-4 w-4 text-slate-400" />
                      <span className="text-slate-900">{user.userName}</span>
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-slate-700">
                      {t(i18nKeyContainer.email)}
                    </label>
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                      <Mail className="h-4 w-4 text-slate-400" />
                      <span className="text-slate-900 break-all">{user.email}</span>
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-slate-700">
                      {t(i18nKeyContainer.staff.role)}
                    </label>
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                      <Shield className="h-4 w-4 text-slate-400" />
                      <span className="text-slate-900">{user.role}</span>
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-slate-700">
                      {t(i18nKeyContainer.staff.status)}
                    </label>
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                      <StatusBadge
                        status={statusLabel}
                        variant={user.isActive ? "success" : "secondary"}
                      />
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-slate-700">
                      {t(i18nKeyContainer.user.registeredOn)}
                    </label>
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-slate-50 border border-slate-200">
                      <Calendar className="h-4 w-4 text-slate-400" />
                      <span className="text-slate-900">{formatDate(user.createdOnUtc)}</span>
                    </div>
                  </div>
                </div>

                <div className="pt-2">
                  <Button onClick={onClose} className="w-full cursor-pointer">
                    {t(i18nKeyContainer.common.close)}
                  </Button>
                </div>
              </div>
            ) : (
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-500">
                {t(i18nKeyContainer.table.noResults)}
              </div>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
