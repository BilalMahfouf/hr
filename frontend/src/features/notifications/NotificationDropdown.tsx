import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { Bell, CheckCheck, Loader2, Monitor } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { useNotifications } from "./use-notifications";
import { useMarkAsRead, useMarkAllAsRead } from "./use-mark-as-read";
import {
    getPushStatus,
    ensurePushSubscription,
    unsubscribeFromPush,
    type PushStatus,
} from "./push-notifications";
import type { Notification } from "./notification-api";

/**
 * Format relative time for notification timestamps
 */
function useRelativeTime() {
    const { t } = useTranslation();

    return useCallback(
        (dateString: string) => {
            const date = new Date(dateString);
            const now = new Date();
            const diffMs = now.getTime() - date.getTime();
            const diffMins = Math.floor(diffMs / (1000 * 60));
            const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
            const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

            if (diffMins < 1) return t(i18nKeyContainer.notification.justNow);
            if (diffMins < 60)
                return t(i18nKeyContainer.notification.minutesAgo, { count: diffMins });
            if (diffHours < 24)
                return t(i18nKeyContainer.notification.hoursAgo, { count: diffHours });
            return t(i18nKeyContainer.notification.daysAgo, { count: diffDays });
        },
        [t]
    );
}

/**
 * Single notification item component
 */
function NotificationItem({
    notification,
    onMarkAsRead,
    isMarking,
}: {
    notification: Notification;
    onMarkAsRead: (id: string) => void;
    isMarking: boolean;
}) {
    const { t } = useTranslation();
    const formatTime = useRelativeTime();

    return (
        <div className="px-4 py-2">
            <div
                className={cn(
                    "p-4 rounded-lg transition-all cursor-pointer",
                    notification.isRead
                        ? "bg-slate-50 hover:bg-slate-100"
                        : "bg-blue-50/70 hover:bg-blue-50 shadow-sm ring-1 ring-blue-100"
                )}
            >
                <div className="flex items-start gap-3">
                    {/* Unread indicator */}
                    <div className="mt-2 shrink-0">
                        {!notification.isRead && (
                            <div className="w-2.5 h-2.5 rounded-full bg-primary shadow-sm" />
                        )}
                        {notification.isRead && <div className="w-2.5 h-2.5" />}
                    </div>

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                        {/* Header row: Title + Time */}
                        <div className="flex items-start justify-between gap-3 mb-2">
                            <h4
                                className={cn(
                                    "text-sm leading-snug",
                                    notification.isRead
                                        ? "font-medium text-slate-600"
                                        : "font-semibold text-slate-900"
                                )}
                            >
                                {notification.title}
                            </h4>
                            <span className="text-xs text-slate-400 whitespace-nowrap shrink-0 mt-0.5">
                                {formatTime(notification.createdOnUtc)}
                            </span>
                        </div>

                        {/* Body text */}
                        <p
                            className={cn(
                                "text-sm leading-relaxed line-clamp-2",
                                notification.isRead
                                    ? "text-slate-500"
                                    : "text-slate-600"
                            )}
                        >
                            {notification.body}
                        </p>

                        {/* Mark as read button for unread notifications */}
                        {!notification.isRead && (
                            <Button
                                variant="ghost"
                                size="sm"
                                className="mt-3 h-7 px-3 text-xs font-medium text-primary hover:text-primary hover:bg-blue-100/50"
                                onClick={(e) => {
                                    e.stopPropagation();
                                    onMarkAsRead(notification.id);
                                }}
                                disabled={isMarking}
                            >
                                {isMarking ? (
                                    <Loader2 className="h-3.5 w-3.5 me-1.5 animate-spin" />
                                ) : (
                                    <CheckCheck className="h-3.5 w-3.5 me-1.5" />
                                )}
                                {t(i18nKeyContainer.notification.markAsRead)}
                            </Button>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

/**
 * Loading skeleton for notifications
 */
function NotificationSkeleton() {
    return (
        <div className="px-4 py-3 bg-white">
            <div className="flex items-start gap-3">
                <Skeleton className="w-2 h-2 rounded-full mt-1.5" />
                <div className="flex-1 space-y-2">
                    <div className="flex justify-between">
                        <Skeleton className="h-4 w-32" />
                        <Skeleton className="h-3 w-12" />
                    </div>
                    <Skeleton className="h-3 w-full" />
                </div>
            </div>
        </div>
    );
}

/**
 * Empty state component
 */
function EmptyState({ isUnreadTab }: { isUnreadTab: boolean }) {
    const { t } = useTranslation();

    return (
        <div className="flex flex-col items-center justify-center py-8 px-4 text-center bg-white">
            <div className="w-12 h-12 rounded-full bg-muted flex items-center justify-center mb-3">
                <Bell className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="text-sm font-medium text-foreground mb-1">
                {t(
                    isUnreadTab
                        ? i18nKeyContainer.notification.noUnreadNotifications
                        : i18nKeyContainer.notification.noNotifications
                )}
            </h3>
            <p className="text-xs text-muted-foreground max-w-xs">
                {t(
                    isUnreadTab
                        ? i18nKeyContainer.notification.noUnreadNotificationsDesc
                        : i18nKeyContainer.notification.noNotificationsDesc
                )}
            </p>
        </div>
    );
}

/**
 * Notification dropdown component for the bell icon
 */
export default function NotificationDropdown() {
    const { t, i18n } = useTranslation();
    const isRtl = i18n.language === "ar";

    const [open, setOpen] = useState(false);
    const [activeTab, setActiveTab] = useState<"all" | "unread">("unread");
    const [pushStatus, setPushStatus] = useState<PushStatus>("disabled");
    const [isPushLoading, setIsPushLoading] = useState(false);

    // Refresh push status whenever the dropdown opens.
    useEffect(() => {
        if (open) {
            getPushStatus().then(setPushStatus);
        }
    }, [open]);

    const handlePushToggle = async () => {
        setIsPushLoading(true);
        try {
            if (pushStatus === "enabled") {
                await unsubscribeFromPush();
                toast.success(t(i18nKeyContainer.toast.notification.pushDisabled));
            } else {
                await ensurePushSubscription();
                toast.success(t(i18nKeyContainer.toast.notification.pushEnabled));
            }
            setPushStatus(await getPushStatus());
        } catch (err: unknown) {
            const message = err instanceof Error ? err.message : "";
            if (message === "push_permission_denied") {
                setPushStatus("denied");
                toast.error(t(i18nKeyContainer.errors.notification.pushBlocked));
            } else {
                toast.error(t(i18nKeyContainer.errors.notification.pushFailed));
            }
        } finally {
            setIsPushLoading(false);
        }
    };

    // Queries
    const {
        data,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
        isLoading,
        isError,
        refetch,
    } = useNotifications(activeTab);

    // Mutations
    const { markAsRead, isMarking } = useMarkAsRead();
    const { markAllAsRead, isMarkingAll } = useMarkAllAsRead();

    // Flatten notifications from all pages
    const notifications = useMemo(
        () => data?.pages.flatMap((page) => page.items) ?? [],
        [data]
    );

    // Count unread notifications (for badge on bell icon)
    const unreadCount = useMemo(
        () => notifications.filter((n) => !n.isRead).length,
        [notifications]
    );

    // Scroll container ref for infinite scroll
    const scrollContainerRef = useRef<HTMLDivElement>(null);

    // Handle scroll for infinite loading
    const handleScroll = useCallback(() => {
        const container = scrollContainerRef.current;
        if (!container || isFetchingNextPage || !hasNextPage) return;

        const { scrollTop, scrollHeight, clientHeight } = container;
        if (scrollHeight - scrollTop - clientHeight < 100) {
            fetchNextPage();
        }
    }, [isFetchingNextPage, hasNextPage, fetchNextPage]);

    // Add scroll listener
    useEffect(() => {
        const container = scrollContainerRef.current;
        if (!container) return;

        container.addEventListener("scroll", handleScroll);
        return () => container.removeEventListener("scroll", handleScroll);
    }, [handleScroll]);

    const handleTabChange = (value: string) => {
        setActiveTab(value as "all" | "unread");
    };

    return (
        <DropdownMenu open={open} onOpenChange={setOpen}>
            <DropdownMenuTrigger asChild>
                <Button
                    variant="ghost"
                    size="icon"
                    className="relative hover:bg-slate-100 cursor-pointer"
                >
                    <Bell className="h-5 w-5 text-slate-600" />
                    {/* Notification badge */}
                    {unreadCount > 0 && (
                        <span className="absolute top-1 end-1 flex items-center justify-center h-4 min-w-4 px-1 text-[10px] font-medium text-white bg-red-500 rounded-full ring-2 ring-white">
                            {unreadCount > 99 ? "99+" : unreadCount}
                        </span>
                    )}
                </Button>
            </DropdownMenuTrigger>

            <DropdownMenuContent
                align={isRtl ? "start" : "end"}
                className="w-80 sm:w-96 p-0 border-white"
            >
                <div dir={isRtl ? "rtl" : "ltr"} className="bg-white">
                {/* Header */}
                <div className="flex items-center justify-between px-4 py-3 bg-white">
                    <h3 className="font-semibold text-foreground">
                        {t(i18nKeyContainer.notification.title)}
                    </h3>
                    {unreadCount > 0 && (
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => markAllAsRead()}
                            disabled={isMarkingAll}
                            className="h-7 px-2 text-xs text-primary hover:text-primary cursor-pointer hover:bg-blue-100/50"
                        >
                            {isMarkingAll ? (
                                <Loader2 className="h-3 w-3 me-1 animate-spin" />
                            ) : (
                                <CheckCheck className="h-3 w-3 me-1" />
                            )}
                            {t(i18nKeyContainer.notification.markAllAsRead)}
                        </Button>
                    )}
                </div>

                {/* Tabs */}
                <Tabs
                    value={activeTab}
                    onValueChange={handleTabChange}
                    dir={isRtl ? "rtl" : "ltr"}
                    className="w-full"
                >
                    <div className="px-4 bg-white">
                        <TabsList className="h-9 bg-white p-0 gap-4">
                            <TabsTrigger
                                value="all"
                                className="h-9 px-1 pb-2 pt-2 rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none bg-transparent cursor-pointer text-sm"
                            >
                                {t(i18nKeyContainer.notification.all)}
                            </TabsTrigger>
                            <TabsTrigger
                                value="unread"
                                className="h-9 px-1 pb-2 pt-2 rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none bg-transparent cursor-pointer gap-2 text-sm"
                            >
                                {t(i18nKeyContainer.notification.unread)}
                                {unreadCount > 0 && (
                                    <Badge
                                        variant="secondary"
                                        className="h-5 min-w-5 px-1.5 text-xs"
                                    >
                                        {unreadCount > 99 ? "99+" : unreadCount}
                                    </Badge>
                                )}
                            </TabsTrigger>
                        </TabsList>
                    </div>

                    {/* Scrollable content */}
                    <div
                        ref={scrollContainerRef}
                        className="max-h-80 overflow-y-auto bg-white"
                    >
                        {/* Loading state */}
                        {isLoading && (
                            <div>
                                {Array.from({ length: 4 }).map((_, i) => (
                                    <NotificationSkeleton key={i} />
                                ))}
                            </div>
                        )}

                        {/* Error state */}
                        {isError && !isLoading && (
                            <div className="flex flex-col items-center justify-center py-8 px-4 text-center">
                                <p className="text-xs text-destructive mb-3">
                                    {t(i18nKeyContainer.errors.notification.fetchFailed)}
                                </p>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => refetch()}
                                    className="h-7 text-xs"
                                >
                                    {t(i18nKeyContainer.common.confirm)}
                                </Button>
                            </div>
                        )}

                        {/* Empty state */}
                        {!isLoading && !isError && notifications.length === 0 && (
                            <EmptyState isUnreadTab={activeTab === "unread"} />
                        )}

                        {/* Notifications list */}
                        {!isLoading && !isError && notifications.length > 0 && (
                            <div>
                                {notifications.map((notification) => (
                                    <NotificationItem
                                        key={notification.id}
                                        notification={notification}
                                        onMarkAsRead={markAsRead}
                                        isMarking={isMarking}
                                    />
                                ))}

                                {/* Load more indicator */}
                                {isFetchingNextPage && (
                                    <div className="flex items-center justify-center py-3">
                                        <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
                                    </div>
                                )}
                            </div>
                        )}
                    </div>
                </Tabs>

                {/* Desktop push notifications footer */}
                {pushStatus !== "unsupported" && (
                    <div className="px-4 py-3 border-t border-slate-100 bg-white">
                        <div className="flex items-center justify-between gap-3">
                            <div className="flex items-center gap-2 min-w-0">
                                <Monitor className="h-4 w-4 shrink-0 text-slate-500" />
                                <div className="min-w-0">
                                    <p className="text-xs font-medium text-slate-700">
                                        {t(i18nKeyContainer.notification.pushTitle)}
                                    </p>
                                    {pushStatus === "denied" && (
                                        <p className="text-xs text-muted-foreground">
                                            {t(i18nKeyContainer.notification.pushBlockedDesc)}
                                        </p>
                                    )}
                                </div>
                            </div>
                            <Button
                                variant={pushStatus === "enabled" ? "secondary" : "outline"}
                                size="sm"
                                disabled={isPushLoading || pushStatus === "denied"}
                                onClick={handlePushToggle}
                                className="h-7 px-3 text-xs shrink-0 cursor-pointer"
                            >
                                {isPushLoading ? (
                                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                                ) : pushStatus === "enabled" ? (
                                    t(i18nKeyContainer.notification.pushDisable)
                                ) : (
                                    t(i18nKeyContainer.notification.pushEnable)
                                )}
                            </Button>
                        </div>
                    </div>
                )}
                </div>
            </DropdownMenuContent>
        </DropdownMenu>
    );
}
