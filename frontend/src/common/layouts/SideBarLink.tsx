import { NavLink } from "react-router-dom";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

export default function SideBarLink({
  content,
  pathname,
  icon: Icon,
  rightIcon: RightIcon,
}:{
    content: string;
    pathname: string;
    icon: LucideIcon;
    rightIcon?: LucideIcon;
}) {
  return (
    <NavLink
      to={pathname}
      className={({ isActive }) =>
        cn(
          "group flex items-center justify-between py-3 px-4 text-sm font-medium rounded-xl transition-all",
          isActive
            ? "bg-primary text-white shadow-lg shadow-primary/20"
            : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
        )
      }
    >
      {({ isActive }) => (
        <>
          <div className="flex items-center gap-3">
            <Icon
              className={cn(
                "h-5 w-5 transition-colors",
                isActive ? "text-white" : "text-slate-400 group-hover:text-slate-600"
              )}
            />
            <span>{content}</span>
          </div>
          {RightIcon && (
            <RightIcon
              className={cn(
                "h-4 w-4 transition-colors",
                isActive ? "text-white" : "text-slate-400 group-hover:text-slate-600"
              )}
            />
          )}
        </>
      )}
    </NavLink>
  );
}
