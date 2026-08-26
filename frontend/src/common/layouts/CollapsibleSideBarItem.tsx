import { useState, useEffect } from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { ChevronRight, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

interface ChildItem {
  label: string;
  pathname: string;
  icon?: LucideIcon;
}

interface CollapsibleSideBarItemProps {
  label: string;
  icon: LucideIcon;
  children: ChildItem[];
  requiresAdmin?: boolean;
  isSidebarCollapsed?: boolean;
}

export default function CollapsibleSideBarItem({
  label,
  icon: Icon,
  children,
  requiresAdmin,
  isSidebarCollapsed = false,
}: CollapsibleSideBarItemProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const [isExpanded, setIsExpanded] = useState(false);

  const isAnyChildActive = children.some((child) => location.pathname === child.pathname || location.pathname.startsWith(child.pathname + "/"));

  useEffect(() => {
    if (isAnyChildActive) {
      setIsExpanded(true);
    }
  }, [isAnyChildActive]);

  const toggleExpanded = () => {
    if (!isSidebarCollapsed) {
      setIsExpanded((prev) => !prev);
    }
  };

  const handleChildClick = (pathname: string) => {
    navigate(pathname);
    if (isSidebarCollapsed) {
      setIsExpanded(false);
    }
  };

  if (requiresAdmin) {
    return null;
  }

  if (isSidebarCollapsed) {
    return (
      <NavLink
        to={children[0]?.pathname ?? "#"}
        className={({ isActive }) =>
          cn(
            "group flex items-center justify-center py-3 px-4 text-sm font-medium rounded-xl transition-all",
            isActive
              ? "bg-primary text-white shadow-lg shadow-primary/20"
              : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          )
        }
        onClick={() => handleChildClick(children[0]?.pathname ?? "#")}
      >
        {({ isActive }) => (
          <Icon
            className={cn(
              "h-5 w-5 transition-colors",
              isActive ? "text-white" : "text-slate-400 group-hover:text-slate-600"
            )}
          />
        )}
      </NavLink>
    );
  }

  return (
    <div className="group">
      <button
        type="button"
        onClick={toggleExpanded}
        className={({ isActive }) =>
          cn(
            "w-full flex items-center justify-between py-3 px-4 text-sm font-medium rounded-xl transition-all",
            isAnyChildActive
              ? "bg-primary text-white shadow-lg shadow-primary/20"
              : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          )
        }
        aria-expanded={isExpanded}
      >
        <div className="flex items-center gap-3">
          <Icon
            className={cn(
              "h-5 w-5 transition-colors",
              isAnyChildActive ? "text-white" : "text-slate-400 group-hover:text-slate-600"
            )}
          />
          <span>{label}</span>
        </div>
        <ChevronRight
          className={cn(
            "h-4 w-4 transition-transform duration-200 text-slate-400",
            isExpanded && "rotate-90"
          )}
        />
      </button>

      <div
        className={cn(
          "overflow-hidden transition-all duration-300 ease-in-out",
          isExpanded ? "max-h-96 opacity-100" : "max-h-0 opacity-0"
        )}
        style={{ maxHeight: isExpanded ? "500px" : "0" }}
      >
        <nav className="space-y-0.5 pl-8 ps-8">
          {children.map((child) => (
            <NavLink
              key={child.pathname}
              to={child.pathname}
              onClick={() => handleChildClick(child.pathname)}
              className={({ isActive }) =>
                cn(
                  "group flex items-center gap-3 py-2 px-3 text-sm font-medium rounded-lg transition-colors",
                  isActive
                    ? "bg-primary/10 text-primary"
                    : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                )
              }
            >
              {({ isActive }) => (
                <>
                  {child.icon && (
                    <child.icon
                      className={cn(
                        "h-4 w-4 transition-colors",
                        isActive ? "text-primary" : "text-slate-400 group-hover:text-slate-600"
                      )}
                    />
                  )}
                  <span>{child.label}</span>
                </>
              )}
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  );
}