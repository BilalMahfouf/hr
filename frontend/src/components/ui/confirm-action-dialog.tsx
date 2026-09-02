import { useTranslation } from "react-i18next";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { FileText } from "lucide-react";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

interface ConfirmActionDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  description: string;
  itemName?: string;
  isLoading?: boolean;
  /** Custom text for confirm button (defaults to common.confirm) */
  confirmAction?: string;
  /** Custom text for loading state (defaults to common.confirm) */
  actionInProgress?: string;
}

/**
 * Reusable confirmation dialog for non-destructive actions (e.g. generate receipt).
 * Mirrors ConfirmDeleteDialog but with neutral primary styling instead of destructive red.
 * Supports RTL/LTR layouts and i18n.
 */
export default function ConfirmActionDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  itemName,
  isLoading = false,
  confirmAction,
  actionInProgress,
}: ConfirmActionDialogProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent
        className="max-w-md p-0 overflow-hidden bg-white"
        dir={isRtl ? "rtl" : "ltr"}
        onInteractOutside={(e) => {
          if (isLoading) e.preventDefault();
        }}
      >
        <div className="p-6">
          <DialogHeader className="space-y-4">
            {/* Action Icon */}
            <div className="flex justify-center">
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                <FileText className="h-6 w-6 text-primary" />
              </div>
            </div>

            {/* Title */}
            <DialogTitle className="text-center text-xl font-semibold text-slate-900">
              {title}
            </DialogTitle>

            {/* Description */}
            <DialogDescription className="text-center text-slate-500">
              {description}
              {itemName && (
                <span className="block mt-2 font-medium text-slate-700">
                  "{itemName}"
                </span>
              )}
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-6 flex gap-3 sm:gap-3">
            <Button
              type="button"
              variant="ghost"
              className="flex-1 h-11 hover:bg-slate-100 transition-colors"
              onClick={onClose}
              disabled={isLoading}
            >
              {t(i18nKeyContainer.common.cancel)}
            </Button>
            <Button
              type="button"
              variant="default"
              className="flex-1 h-11 font-bold shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all"
              onClick={onConfirm}
              disabled={isLoading}
            >
              {isLoading
                ? (actionInProgress ?? t(i18nKeyContainer.common.confirm))
                : (confirmAction ?? t(i18nKeyContainer.common.confirm))}
            </Button>
          </DialogFooter>
        </div>
      </DialogContent>
    </Dialog>
  );
}
