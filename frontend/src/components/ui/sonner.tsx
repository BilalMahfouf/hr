import { useTranslation } from "react-i18next";
import { Toaster as Sonner } from "sonner";

type ToasterProps = React.ComponentProps<typeof Sonner>;

const Toaster = ({ ...props }: ToasterProps) => {
  const { i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  return (
    <Sonner
      theme="light"
      position={isRtl ? "bottom-left" : "bottom-right"}
      dir={isRtl ? "rtl" : "ltr"}
      className="toaster group"
      toastOptions={{
        classNames: {
          toast:
            "group toast group-[.toaster]:bg-white group-[.toaster]:text-slate-900 group-[.toaster]:border-slate-200 group-[.toaster]:shadow-lg group-[.toaster]:rounded-lg group-[.toaster]:p-4",
          title: "group-[.toast]:text-slate-900 group-[.toast]:font-semibold",
          description: "group-[.toast]:text-slate-500",
          actionButton:
            "group-[.toast]:bg-primary group-[.toast]:text-primary-foreground",
          cancelButton:
            "group-[.toast]:bg-slate-100 group-[.toast]:text-slate-500",
          closeButton:
            "group-[.toast]:bg-slate-100 group-[.toast]:text-slate-500 group-[.toast]:border-slate-200 group-[.toast]:hover:bg-slate-200",
          success:
            "group-[.toaster]:!bg-green-50 group-[.toaster]:!border-green-200 group-[.toaster]:!text-green-800 [&_[data-title]]:!text-green-800 [&_[data-description]]:!text-green-600",
          error:
            "group-[.toaster]:!bg-red-50 group-[.toaster]:!border-red-200 group-[.toaster]:!text-red-800 [&_[data-title]]:!text-red-800 [&_[data-description]]:!text-red-600",
          warning:
            "group-[.toaster]:!bg-amber-50 group-[.toaster]:!border-amber-200 group-[.toaster]:!text-amber-800 [&_[data-title]]:!text-amber-800 [&_[data-description]]:!text-amber-600",
          info: "group-[.toaster]:!bg-blue-50 group-[.toaster]:!border-blue-200 group-[.toaster]:!text-blue-800 [&_[data-title]]:!text-blue-800 [&_[data-description]]:!text-blue-600",
        },
      }}
      closeButton
      richColors
      duration={4000}
      {...props}
    />
  );
};

export { Toaster };
