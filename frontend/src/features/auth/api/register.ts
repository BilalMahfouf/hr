import api from "@/lib/api/api";
import { useMutation } from "@tanstack/react-query";

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  userName: string; // backend uses userName
  email: string;
  password: string;
}

export const register = async (data: RegisterRequest): Promise<void> => {
  await api.post("/auth/register", data);
};

export const useRegister = () => {
    return useMutation({
        mutationFn: register,
    });
};
