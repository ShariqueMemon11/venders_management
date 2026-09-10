import api from './apiClient';
import type { 
    Vendor, VendorContact, VendorAddress, BankAccount, 
    VendorContract, VendorPerformance, VendorRisk, VendorCompliance 
} from '../types/index';

export const VendorService = {
    getDashboardSummary: async () => {
        const response = await api.get('/vendors/dashboard/summary');
        return response.data;
    },
    getVendors: async () => {
        const response = await api.get('/vendors');
        return response.data;
    },
    getPagedVendors: async (
        page: number = 1,
        pageSize: number = 10,
        filters?: { name?: string; status?: string | number; country?: string }
    ) => {
        const params = new URLSearchParams();
        params.set('page', String(page));
        params.set('pageSize', String(pageSize));
        if (filters?.name?.trim()) params.set('name', filters.name.trim());
        if (filters?.status !== undefined && filters?.status !== '' && filters?.status !== null) {
            params.set('status', String(filters.status));
        }
        if (filters?.country?.trim()) params.set('country', filters.country.trim());
        const response = await api.get(`/vendors/directory?${params.toString()}`);
        return response.data;
    },
    getVendorById: async (id: string) => {
        const response = await api.get(`/vendors/${id}/details`);
        return response.data;
    },
    createVendor: async (vendor: Partial<Vendor>) => {
        // Draft-then-submit: bare create (no workflow). Submit later via submitForApproval.
        const response = await api.post('/vendors', vendor);
        return response.data;
    },
    updateVendor: async (id: string, vendor: Partial<Vendor>) => {
        const response = await api.put(`/vendors/${id}`, vendor);
        return response.data;
    },
    deleteVendor: async (id: string) => {
        const response = await api.post(`/vendors/${id}/terminate`);
        return response.data;
    },
    terminateVendor: async (id: string) => {
        const response = await api.post(`/vendors/${id}/terminate`);
        return response.data;
    },

    // Contacts
    addContact: async (vendorId: string, contact: Partial<VendorContact>) => {
        const response = await api.post(`/vendors/${vendorId}/contacts`, contact);
        return response.data;
    },
    updateContact: async (contactId: string, contact: Partial<VendorContact>) => {
        const response = await api.put(`/vendors/contacts/${contactId}`, contact);
        return response.data;
    },
    deleteContact: async (contactId: string) => {
        const response = await api.delete(`/vendors/contacts/${contactId}`);
        return response.data;
    },

    // Addresses
    addAddress: async (vendorId: string, address: Partial<VendorAddress>) => {
        const response = await api.post(`/vendors/${vendorId}/addresses`, address);
        return response.data;
    },
    updateAddress: async (addressId: string, address: Partial<VendorAddress>) => {
        const response = await api.put(`/vendors/addresses/${addressId}`, address);
        return response.data;
    },
    deleteAddress: async (addressId: string) => {
        const response = await api.delete(`/vendors/addresses/${addressId}`);
        return response.data;
    },

    // Banking
    addBankAccount: async (vendorId: string, account: Partial<BankAccount>) => {
        const response = await api.post(`/vendors/${vendorId}/bankaccounts`, account);
        return response.data;
    },
    updateBankAccount: async (accountId: string, account: Partial<BankAccount>) => {
        const response = await api.put(`/vendors/bankaccounts/${accountId}`, account);
        return response.data;
    },
    deleteBankAccount: async (accountId: string) => {
        const response = await api.delete(`/vendors/bankaccounts/${accountId}`);
        return response.data;
    },

    // Contracts
    addContract: async (vendorId: string, contract: Partial<VendorContract>) => {
        const response = await api.post(`/vendors/${vendorId}/contracts`, contract);
        return response.data;
    },
    updateContract: async (contractId: string, contract: Partial<VendorContract>) => {
        const response = await api.put(`/vendors/contracts/${contractId}`, contract);
        return response.data;
    },
    deleteContract: async (contractId: string) => {
        const response = await api.delete(`/vendors/contracts/${contractId}`);
        return response.data;
    },

    // Compliance
    addCompliance: async (vendorId: string, compliance: Partial<VendorCompliance>) => {
        const response = await api.post(`/vendors/${vendorId}/compliance`, compliance);
        return response.data;
    },
    deleteCompliance: async (complianceId: string) => {
        const response = await api.delete(`/vendors/compliance/${complianceId}`);
        return response.data;
    },

    // Performance
    addPerformance: async (vendorId: string, perf: Partial<VendorPerformance>) => {
        const response = await api.post(`/vendors/${vendorId}/performances`, perf);
        return response.data;
    },
    deletePerformance: async (perfId: string) => {
        const response = await api.delete(`/vendors/performances/${perfId}`);
        return response.data;
    },

    // Risks
    addRisk: async (vendorId: string, risk: Partial<VendorRisk>) => {
        const response = await api.post(`/vendors/${vendorId}/risks`, risk);
        return response.data;
    },
    deleteRisk: async (riskId: string) => {
        const response = await api.delete(`/vendors/risks/${riskId}`);
        return response.data;
    },
    
    // Documents
    uploadDocument: async (vendorId: string, formData: FormData) => {
        // FormData is used to handle file uploads
        const response = await api.post(`/vendors/${vendorId}/documents/upload`, formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
        return response.data;
    },
    deleteDocument: async (docId: string) => {
        const response = await api.delete(`/vendors/documents/${docId}`);
        return response.data;
    },
    downloadDocument: async (docId: string, fileName?: string) => {
        const response = await api.get(`/vendors/documents/${docId}/download`, {
            responseType: 'blob',
        });
        const blob = new Blob([response.data], {
            type: response.headers['content-type'] || 'application/octet-stream',
        });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName || 'document';
        link.click();
        URL.revokeObjectURL(link.href);
    },
    updateDocumentStatus: async (docId: string, status: number, comments?: string) => {
        const params = new URLSearchParams({ status: String(status) });
        if (comments) params.set('comments', comments);
        const response = await api.patch(`/vendors/documents/${docId}/status?${params.toString()}`);
        return response.data;
    },
    // Workflow
    submitForApproval: async (vendorId: string) => {
        const response = await api.post(`/vendors/${vendorId}/submit-for-approval`);
        return response.data;
    },
    getActiveRequest: async (vendorId: string) => {
        const response = await api.get(`/vendors/${vendorId}/active-request`);
        return response.data;
    },
    processWorkflowAction: async (requestId: string, action: string, comments: string) => {
        const response = await api.post(`/vendors/requests/${requestId}/workflow?action=${action}`, `"${comments}"`, {
            headers: { 'Content-Type': 'application/json' }
        });
        return response.data;
    }
};