import api from "@/lib/api/api.ts";

const dashboardApi = {
    getDashboardCard: async (): Promise<DashboardCardsResponse> => {
        const response = await api.get<DashboardCardsResponse>('/dashboard/cards');
        if (response.status !== 200) {
            throw new Error('Failed to fetch dashboard cards');
        }
        return response.data;
    }
}

export type DashboardCardsResponse = Record<string, never>;

export default dashboardApi;
