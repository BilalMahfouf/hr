import { useTranslation } from "react-i18next";
import { LayoutDashboard } from "lucide-react";

import { Card } from "@/components/ui/card";

export default function DashboardPage() {
    const { t, i18n } = useTranslation();
    const isRtl = i18n.language === "ar";

    return (
        <div dir={isRtl ? "rtl" : "ltr"} className="min-h-full">
            {/* Page Header */}
            <div className="flex flex-col sm:flex-row sm:items-center gap-4 mb-8">
                {/* Title Section */}
                <div className="flex items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                        <LayoutDashboard className="h-6 w-6 text-primary" />
                    </div>
                    <div>
                        <h1 className="text-2xl font-bold text-slate-900">
                            {t("dashboardPage.title")}
                        </h1>
                        <p className="text-slate-500 text-sm">
                            {t("dashboardPage.subtitle")}
                        </p>
                    </div>
                </div>
            </div>

            {/* Welcome Message */}
            <Card className="p-8 rounded-2xl bg-white border-0 shadow-sm">
                <p className="text-slate-600">
                    {t("dashboardPage.welcome") || "Welcome to your dashboard"}
                </p>
            </Card>
        </div>
    );
}