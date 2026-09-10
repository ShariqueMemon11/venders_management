import api from './apiClient';

export type AppNotification = {
    id: string;
    title: string;
    message: string;
    type: string;
    severity: string;
    referenceType?: string;
    referenceId?: string;
    isRead: boolean;
    readAt?: string;
    createdAt: string;
    actionUrl?: string;
};

export const NotificationService = {
    list: async (unreadOnly = false, take = 50) => {
        const response = await api.get(`/notifications?unreadOnly=${unreadOnly}&take=${take}`);
        return response.data;
    },
    unreadCount: async () => {
        const response = await api.get('/notifications/unread-count');
        return response.data;
    },
    markRead: async (id: string) => {
        const response = await api.post(`/notifications/${id}/read`);
        return response.data;
    },
    markAllRead: async () => {
        const response = await api.post('/notifications/read-all');
        return response.data;
    },
    remove: async (id: string) => {
        const response = await api.delete(`/notifications/${id}`);
        return response.data;
    }
};
