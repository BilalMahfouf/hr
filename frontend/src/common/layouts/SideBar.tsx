import {
  LayoutDashboard,
  Settings,
  LogOut,
  Languages,
  Shield,
  CreditCard,
  ScanLine,
  CalendarClock,
  Users,
} from "lucide-react";
import SideBarLink from "./SideBarLink";
import CollapsibleSideBarItem from "./CollapsibleSideBarItem";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useMutation } from "@tanstack/react-query";
import { authApi } from "@/lib/api/auth";
import { useNavigate } from "react-router-dom";
import { useCurrentUser } from "@/features/auth/useCurrentUser";

type NavLinkItem = {
  pathname: string;
  key: string;
  icon: typeof LayoutDashboard;
  requiresAdmin?: boolean;
};

type CollapsibleItem = {
  labelKey: string;
  icon: typeof Users;
  children: { pathname: string; key: string }[];
};

const navigationItems: (NavLinkItem | CollapsibleItem)[] = [
  { pathname: "/dashboard", key: i18nKeyContainer.dashboard, icon: LayoutDashboard },
  { pathname: "/users", key: i18nKeyContainer.staff.title, icon: Shield, requiresAdmin: true },
  { pathname: "/subscription-plans", key: i18nKeyContainer.subscriptionPlansNav, icon: CreditCard, requiresAdmin: true },
  { pathname: "/machines", key: i18nKeyContainer.machines.title, icon: ScanLine, requiresAdmin: true },
  {
    labelKey: i18nKeyContainer.attendance.title,
    icon: CalendarClock,
    children: [
      { pathname: "/attendance/punches", key: i18nKeyContainer.attendance.punches.title },
      { pathname: "/attendance/records", key: i18nKeyContainer.attendance.records.title },
    ],
  },
  {
    labelKey: i18nKeyContainer.employees.title,
    icon: Users,
    children: [
      { pathname: "/employees", key: i18nKeyContainer.employees.title },
      { pathname: "/employee-groups", key: i18nKeyContainer.employeeGroups.title },
    ],
  },
  { pathname: "/settings", key: i18nKeyContainer.settings, icon: Settings },
];

export default function Sidebar({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const { data: currentUser } = useCurrentUser();

  const canAccessUsers = currentUser?.role?.toLowerCase() === "admin";

  const mutation = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      navigate("/");
    },
  });

  const handleLogOut = () => {
    mutation.mutate();
  };

  return (
    <>
      {/* Sidebar */}
      <div
        className={cn(
          "fixed top-0 bottom-0 w-80 bg-white border-e border-slate-200 z-50 transform transition-all duration-500 ease-in-out",
          isOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0",
          isRtl ? "end-0 lg:end-auto lg:start-0" : "start-0"
        )}
      >
        <div className="flex flex-col h-full bg-white">
          {/* Header */}
          <div className="p-6 bg-white">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <img src="/logo.jpg" alt={t(i18nKeyContainer.app.logoAlt)} className="w-12 h-12 rounded-xl object-cover shadow-lg" />
                <span className="text-xl font-bold text-slate-900">{t(i18nKeyContainer.app.name)}</span>
              </div>
              <Button variant="ghost" size="icon" onClick={onClose} className="lg:hidden hover:bg-slate-100">
                <Languages className="h-5 w-5 text-slate-600" />
              </Button>
            </div>
          </div>

          {/* Navigation */}
          <nav className="flex-1 overflow-y-auto p-4 space-y-1 bg-white">
            {navigationItems
              .filter((item) => !("requiresAdmin" in item) || !item.requiresAdmin || canAccessUsers)
              .map((item) => {
                if ("children" in item) {
                  return (
                    <CollapsibleSideBarItem
                      key={item.children[0]?.pathname ?? item.labelKey}
                      label={t(item.labelKey)}
                      icon={item.icon}
                      items={item.children.map((child) => ({
                        label: t(child.key),
                        pathname: child.pathname,
                      }))}
                    />
                  );
                }
                return (
                  <SideBarLink
                    key={item.pathname}
                    pathname={item.pathname}
                    content={t(item.key)}
                    icon={item.icon}
                  />
                );
              })}
          </nav>

          {/* User Profile Footer */}
          <div className="p-4 bg-white">
            <Button
              variant="ghost"
              className="cursor-pointer w-full justify-start gap-3 text-slate-600 hover:text-red-600 hover:bg-red-50"
              onClick={handleLogOut}
            >
              <LogOut className="h-4 w-4" />
              <span>{t(i18nKeyContainer.logout)}</span>
            </Button>
          </div>
        </div>
      </div>

      {/* Overlay for mobile */}
      {isOpen && (
        <div className="fixed inset-0 bg-black/20 backdrop-blur-sm z-40 lg:hidden" onClick={onClose} />
      )}
    </>
  );
}