import api from './apiClient';
import type { CreateUserRequest } from '../types/index';

export const UserService = {
    listUsers: async () => {
        const response = await api.get('/users');
        return response.data;
    },
    createUser: async (user: CreateUserRequest) => {
        const response = await api.post('/users', user);
        return response.data;
    },
    deactivateUser: async (id: string) => {
        const response = await api.patch(`/users/${id}/deactivate`);
        return response.data;
    },
    activateUser: async (id: string) => {
        const response = await api.patch(`/users/${id}/activate`);
        return response.data;
    },
    terminateUser: async (id: string) => {
        const response = await api.patch(`/users/${id}/terminate`);
        return response.data;
    },
};
