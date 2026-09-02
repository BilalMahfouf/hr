import { useState, useCallback } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation } from "@tanstack/react-query";
import { CalendarClock, Search, User, Loader2 } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceApi, { type ImportAttendanceResponse } from "./attendance-api";
import attendanceMachineApi from "@/features/machines/attendance-machine-api";
import employeesApi, { type EmployeeListItem } from "@/features/employees/employees-api";
import DateInput from "@/features/employees/DateInput";
import SelectInput from "@/features/employees/SelectInput";
import { useToast } from "@/hooks/use-toast";

function getToday(): string {
  return new Date().toISOString().split("T")[0];
}

function getDaysBetween(a: string, b: string): number {
  const d1 = new Date(a);
  const d2 = new Date(b);
  const diff = Math.abs(d2.getTime() - d1.getTime());
  return Math.ceil(diff / (1000 * 60 * 60 * 24));
}

export default function ImportAttendancePage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const { handleApiError, success } = useToast();

  const [fromDate, setFromDate] = useState(getToday());
  const [toDate, setToDate] = useState(getToday());
  const [dateError, setDateError] = useState<string | null>(null);

  const [employeeSearchValue, setEmployeeSearchValue] = useState("");
  const [foundEmployee, setFoundEmployee] = useState<EmployeeListItem | null>(null);
  const [employeeNotFound, setEmployeeNotFound] = useState(false);

  const [selectedMachineId, setSelectedMachineId] = useState("");

  const validateDates = useCallback(
    (from: string, to: string) => {
      if (!from || !to) {
        setDateError(null);
        return false;
      }
      if (getDaysBetween(from, to) > 7) {
        setDateError(t(i18nKeyContainer.attendance.import.maxOneWeek));
        return false;
      }
      setDateError(null);
      return true;
    },
    [t],
  );

  const handleFromChange = (value: string) => {
    setFromDate(value);
    validateDates(value, toDate);
  };

  const handleToChange = (value: string) => {
    setToDate(value);
    validateDates(fromDate, value);
  };

  const datesValid = fromDate && toDate && !dateError && getDaysBetween(fromDate, toDate) <= 7;

  const importMutation = useMutation({
    mutationFn: (request: { from: string; to: string }) =>
      attendanceApi.importAllMachines(request),
    onSuccess: (data: ImportAttendanceResponse) => {
      success(t(i18nKeyContainer.attendance.import.result.success), {
        description: `${t(i18nKeyContainer.attendance.import.result.machinesCount, { count: data.machineCount })} | ${t(i18nKeyContainer.attendance.import.result.punchesCount, { count: data.punchCount })}`,
      });
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.errors.attendance.importFailed);
    },
  });

  const importByEmployeeMutation = useMutation({
    mutationFn: ({ employeeId, request }: { employeeId: string; request: { from: string; to: string } }) =>
      attendanceApi.importByEmployee(employeeId, request),
    onSuccess: (data: ImportAttendanceResponse) => {
      success(t(i18nKeyContainer.attendance.import.result.success), {
        description: `${t(i18nKeyContainer.attendance.import.result.machinesCount, { count: data.machineCount })} | ${t(i18nKeyContainer.attendance.import.result.punchesCount, { count: data.punchCount })}`,
      });
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.errors.attendance.importFailed);
    },
  });

  const importByMachineMutation = useMutation({
    mutationFn: ({ machineId, request }: { machineId: string; request: { from: string; to: string } }) =>
      attendanceApi.importByMachine(machineId, request),
    onSuccess: (data: ImportAttendanceResponse) => {
      success(t(i18nKeyContainer.attendance.import.result.success), {
        description: `${t(i18nKeyContainer.attendance.import.result.machinesCount, { count: data.machineCount })} | ${t(i18nKeyContainer.attendance.import.result.punchesCount, { count: data.punchCount })}`,
      });
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.errors.attendance.importFailed);
    },
  });

  const { data: machinesData, isLoading: machinesLoading } = useQuery({
    queryKey: ["machines-select"],
    queryFn: () =>
      attendanceMachineApi.getAllMachines({ page: 1, pageSize: 1000 }),
  });

  const machines = machinesData?.item ?? [];

  const machineOptions = machines.map((m) => ({
    value: m.machineId,
    label: `${m.ipAddress} (#${m.machineNumber})`,
  }));

  const handleEmployeeSearch = useCallback(async () => {
    const q = employeeSearchValue.trim();
    if (!q) return;
    setEmployeeNotFound(false);
    setFoundEmployee(null);
    try {
      const result = await employeesApi.getEmployeeById(q);
      setFoundEmployee(result);
    } catch {
      setEmployeeNotFound(true);
    }
  }, [employeeSearchValue]);

  const handleEmployeeKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleEmployeeSearch();
    }
  };

  const handleImportAll = () => {
    if (!datesValid) return;
    importMutation.mutate({ from: fromDate, to: toDate });
  };

  const handleImportByEmployee = () => {
    if (!datesValid || !foundEmployee) return;
    importByEmployeeMutation.mutate({
      employeeId: foundEmployee.matricule,
      request: { from: fromDate, to: toDate },
    });
  };

  const handleImportByMachine = () => {
    if (!datesValid || !selectedMachineId) return;
    importByMachineMutation.mutate({
      machineId: selectedMachineId,
      request: { from: fromDate, to: toDate },
    });
  };

  const isAnyImporting =
    importMutation.isPending ||
    importByEmployeeMutation.isPending ||
    importByMachineMutation.isPending;

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
            <CalendarClock className="h-5 w-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-slate-900">
              {t(i18nKeyContainer.attendance.import.title)}
            </h1>
            <p className="text-slate-500">
              {t(i18nKeyContainer.attendance.import.description)}
            </p>
          </div>
        </div>
      </div>

      <Tabs defaultValue="allMachines" className="w-full">
        <TabsList className="mb-4">
          <TabsTrigger value="allMachines">
            {t(i18nKeyContainer.attendance.import.tabs.allMachines)}
          </TabsTrigger>
          <TabsTrigger value="byEmployee">
            {t(i18nKeyContainer.attendance.import.tabs.byEmployee)}
          </TabsTrigger>
          <TabsTrigger value="byMachine">
            {t(i18nKeyContainer.attendance.import.tabs.byMachine)}
          </TabsTrigger>
        </TabsList>

        {/* All Machines Tab */}
        <TabsContent value="allMachines">
          <Card className="border-slate-200 bg-white shadow-sm">
            <CardContent className="pt-6">
              <div className="space-y-4">
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <DateInput
                    id="from-all"
                    label={t(i18nKeyContainer.attendance.import.dateRange.from)}
                    value={fromDate}
                    onChange={handleFromChange}
                    error={dateError ?? undefined}
                    disabled={isAnyImporting}
                    required
                  />
                  <DateInput
                    id="to-all"
                    label={t(i18nKeyContainer.attendance.import.dateRange.to)}
                    value={toDate}
                    onChange={handleToChange}
                    error={dateError ?? undefined}
                    disabled={isAnyImporting}
                    required
                  />
                </div>
                <div className="flex justify-end">
                  <Button
                    onClick={handleImportAll}
                    disabled={!datesValid || isAnyImporting}
                    className="h-11 px-6"
                  >
                    {importMutation.isPending ? (
                      <>
                        <Loader2 className="me-2 h-4 w-4 animate-spin" />
                        {t(i18nKeyContainer.attendance.import.importing)}
                      </>
                    ) : (
                      t(i18nKeyContainer.attendance.import.importButton)
                    )}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* By Employee Tab */}
        <TabsContent value="byEmployee">
          <Card className="border-slate-200 bg-white shadow-sm">
            <CardContent className="pt-6">
              <div className="space-y-4">
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <Search className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400 pointer-events-none" />
                    <Input
                      value={employeeSearchValue}
                      onChange={(e) => setEmployeeSearchValue(e.target.value)}
                      onKeyDown={handleEmployeeKeyDown}
                      placeholder={t(i18nKeyContainer.attendance.import.employeeSearch.placeholder)}
                      disabled={isAnyImporting}
                      className="h-11 border-slate-200 bg-slate-50 ps-10 focus:bg-white"
                    />
                  </div>
                  <Button
                    variant="default"
                    onClick={handleEmployeeSearch}
                    disabled={!employeeSearchValue.trim() || isAnyImporting}
                    className="h-11 px-6"
                  >
                    {t(i18nKeyContainer.attendance.import.employeeSearch.search)}
                  </Button>
                </div>

                {employeeNotFound && (
                  <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-700">
                    {t(i18nKeyContainer.attendance.import.employeeSearch.notFound)}
                  </div>
                )}

                {foundEmployee && (
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
                    <div className="flex items-center gap-3 mb-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
                        <User className="h-5 w-5 text-primary" />
                      </div>
                      <h3 className="text-sm font-semibold text-slate-900">
                        {t(i18nKeyContainer.attendance.import.employeeCard.name)}
                      </h3>
                    </div>
                    <div className="grid grid-cols-2 gap-3 text-sm">
                      <div>
                        <span className="text-slate-500">
                          {t(i18nKeyContainer.attendance.import.employeeCard.id)}:
                        </span>
                        <span className="ms-2 font-medium text-slate-900">
                          {foundEmployee.matricule}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-500">
                          {t(i18nKeyContainer.attendance.import.employeeCard.name)}:
                        </span>
                        <span className="ms-2 font-medium text-slate-900">
                          {foundEmployee.firstName} {foundEmployee.lastName}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-500">
                          {t(i18nKeyContainer.attendance.import.employeeCard.department)}:
                        </span>
                        <span className="ms-2 font-medium text-slate-900">
                          {foundEmployee.department ?? "—"}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-500">
                          {t(i18nKeyContainer.attendance.import.employeeCard.group)}:
                        </span>
                        <span className="ms-2 font-medium text-slate-900">
                          {foundEmployee.group ?? "—"}
                        </span>
                      </div>
                    </div>
                  </div>
                )}

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <DateInput
                    id="from-employee"
                    label={t(i18nKeyContainer.attendance.import.dateRange.from)}
                    value={fromDate}
                    onChange={handleFromChange}
                    error={dateError ?? undefined}
                    disabled={isAnyImporting}
                    required
                  />
                  <DateInput
                    id="to-employee"
                    label={t(i18nKeyContainer.attendance.import.dateRange.to)}
                    value={toDate}
                    onChange={handleToChange}
                    error={dateError ?? undefined}
                    disabled={isAnyImporting}
                    required
                  />
                </div>

                <div className="flex justify-end">
                  <Button
                    onClick={handleImportByEmployee}
                    disabled={!datesValid || !foundEmployee || isAnyImporting}
                    className="h-11 px-6"
                  >
                    {importByEmployeeMutation.isPending ? (
                      <>
                        <Loader2 className="me-2 h-4 w-4 animate-spin" />
                        {t(i18nKeyContainer.attendance.import.importing)}
                      </>
                    ) : (
                      t(i18nKeyContainer.attendance.import.importButton)
                    )}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* By Machine Tab */}
        <TabsContent value="byMachine">
          <Card className="border-slate-200 bg-white shadow-sm">
            <CardContent className="pt-6">
              <div className="space-y-4">
                {machinesLoading ? (
                  <Skeleton className="h-11 w-full" />
                ) : (
                  <SelectInput
                    id="machine-select"
                    label={t(i18nKeyContainer.attendance.import.tabs.byMachine)}
                    value={selectedMachineId}
                    onChange={setSelectedMachineId}
                    options={machineOptions}
                    placeholder={t(i18nKeyContainer.attendance.import.machineSelect.placeholder)}
                    disabled={isAnyImporting}
                    required
                  />
                )}

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <DateInput
                    id="from-machine"
                    label={t(i18nKeyContainer.attendance.import.dateRange.from)}
                    value={fromDate}
                    onChange={handleFromChange}
                    error={dateError ?? undefined}
                    disabled={isAnyImporting}
                    required
                  />
                  <DateInput
                    id="to-machine"
                    label={t(i18nKeyContainer.attendance.import.dateRange.to)}
                    value={toDate}
                    onChange={handleToChange}
                    error={dateError ?? undefined}
                    disabled={isAnyImporting}
                    required
                  />
                </div>

                <div className="flex justify-end">
                  <Button
                    onClick={handleImportByMachine}
                    disabled={!datesValid || !selectedMachineId || isAnyImporting}
                    className="h-11 px-6"
                  >
                    {importByMachineMutation.isPending ? (
                      <>
                        <Loader2 className="me-2 h-4 w-4 animate-spin" />
                        {t(i18nKeyContainer.attendance.import.importing)}
                      </>
                    ) : (
                      t(i18nKeyContainer.attendance.import.importButton)
                    )}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
