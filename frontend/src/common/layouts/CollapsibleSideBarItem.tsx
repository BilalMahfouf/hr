import { useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { ChevronDown } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export interface SidebarChildItem {
  label: string;
  pathname: string;
  icon?: LucideIcon;
}

interface CollapsibleSideBarItemProps {
  label: string;
  icon: LucideIcon;
  items: SidebarChildItem[];
}

/**
 * Reusable collapsible sidebar navigation item.
 * The parent is a real <button> that toggles its child links.
 * Auto-expands when any child route is active.
 */
export default function CollapsibleSideBarItem({
  label,
  icon: Icon,
  items,
}: CollapsibleSideBarItemProps) {
  const location = useLocation();
  const [isExpanded, setIsExpanded] = useState(() =>
    items.some((c) => location.pathname.startsWith(c.pathname)),
  );

  const isAnyChildActive = items.some(
    (child) =>
      location.pathname === child.pathname ||
      location.pathname.startsWith(child.pathname + "/"),
  );

  return (
    <div>
      {/* Parent toggle button */}
      <button
        type="button"
        onClick={() => setIsExpanded((prev) => !prev)}
        aria-expanded={isExpanded}
        className={cn(
          "group flex w-full items-center justify-between py-3 px-4 text-sm font-medium rounded-xl transition-all cursor-pointer",
          isAnyChildActive
            ? "bg-primary text-white shadow-lg shadow-primary/20"
            : "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
        )}
      >
        <div className="flex items-center gap-3">
          <Icon
            className={cn(
              "h-5 w-5 transition-colors",
              isAnyChildActive ? "text-white" : "text-slate-400 group-hover:text-slate-600",
            )}
          />
          <span>{label}</span>
        </div>
        <ChevronDown
          className={cn(
            "h-4 w-4 shrink-0 transition-transform duration-200",
            isAnyChildActive ? "text-white" : "text-slate-400 group-hover:text-slate-600",
            isExpanded && "rotate-180",
          )}
        />
      </button>

      {/* Submenu with smooth expand/collapse */}
      <div
        className={cn(
          "overflow-hidden transition-all duration-300 ease-in-out",
          isExpanded ? "max-h-96 opacity-100 mt-1 space-y-0.5" : "max-h-0 opacity-0",
        )}
      >
        {items.map((child) => (
          <NavLink
            key={child.pathname}
            to={child.pathname}
            end={location.pathname === child.pathname}
            className={({ isActive }) =>
              cn(
                "group flex items-center gap-3 py-2 ps-8 pe-3 mx-1 rounded-lg text-sm font-medium transition-colors",
                isActive || location.pathname.startsWith(child.pathname + "/")
                  ? "bg-primary/10 text-primary"
                  : "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
              )
            }
          >
            {({ isActive }) => (
              <>
                {child.icon && (
                  <child.icon
                    className={cn(
                      "h-4 w-4 shrink-0 transition-colors",
                      isActive ? "text-primary" : "text-slate-400 group-hover:text-slate-600",
                    )}
                  />
                )}
                <span>{child.label}</span>
              </>
            )}
          </NavLink>
        ))}
      </div>
    </div>
  );
}