import type { PagedList, TableRequest } from "@/components/tables";
import api from "@/lib/api/api";
import { getTableRequsestParams } from "@/lib/utils";
import i18n from "@/lib/i18n";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export type EmployeeListItem = {
  matricule: string;
  bdg: string | null;
  firstName: string;
  lastName: string;
  group: string | null;
  department: string | null;
  phone: string | null;
};

export type EmployeeDetails = EmployeeListItem & {
  birthDate: string | null;
  birthPlace: string | null;
  sex: string | null;
  address: string | null;
  nationality: string | null;
  codeNiv: string | null;
  spec: string | null;
  photoBase64: string | null;
};

const employeesApi = {
  getAllEmployees: async (
    request: TableRequest,
  ): Promise<PagedList<EmployeeListItem>> => {
    const params = getTableRequsestParams(request);
    const result = await api.get<PagedList<EmployeeListItem>>("/employees", {
      params,
    });

    if (result.status !== 200) {
      throw new Error(
        i18n.t(i18nKeyContainer.errors.employee.fetchEmployees),
      );
    }

    return result.data;
  },

  getEmployeeById: async (id: string): Promise<EmployeeDetails> => {
    const result = await api.get<EmployeeDetails>(`/employees/${id}`);

    if (result.status !== 200) {
      throw new Error(i18n.t(i18nKeyContainer.errors.employee.fetchEmployee));
    }

    return result.data;
  },
};

export default employeesApi;
