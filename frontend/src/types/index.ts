export interface Vendor {
    id?: string;
    vendorNumber?: string;
    legalName: string;
    tradeName?: string;
    status?: number | string;
    taxRegistrationNumber?: string;
    website?: string;
    currencyCode?: string;
    createdAt?: string;
}

export interface VendorContact {
    id?: string;
    vendorId?: string;
    name: string;
    jobTitle?: string;
    email: string;
    phone?: string;
    mobile?: string;
    contactType: number | string;
    contactTypeName?: string;
    isPrimary: boolean;
    isActive: boolean;
}

export interface VendorAddress {
    id?: string;
    vendorId?: string;
    addressType: number | string;
    addressTypeName?: string;
    addressLine1: string;
    addressLine2?: string;
    city: string;
    stateProvince?: string;
    country: string;
    postalCode?: string;
}

export interface BankAccount {
    id?: string;
    vendorId?: string;
    bankName: string;
    accountName?: string;
    accountNumber: string;
    iban?: string;
    swiftBic?: string;
    currencyCode?: string;
    isVerified?: boolean;
    isApproved?: boolean;
}

export interface VendorDocument {
    id?: string;
    vendorId?: string;
    title: string;
    documentType: number | string;
    documentTypeName?: string;
    storagePath?: string;
    originalFileName?: string;
    contentType?: string;
    fileSize?: number;
    expirationDate?: string;
    status?: number | string;
    statusName?: string;
    verificationComments?: string;
}

export interface VendorContract {
    id?: string;
    vendorId?: string;
    contractNumber: string;
    title: string;
    startDate: string;
    endDate: string;
    contractValue: number;
    currencyCode?: string;
    paymentTerms?: string;
    slaBrief?: string;
    status?: number | string;
    statusName?: string;
}

export interface VendorCompliance {
    id?: string;
    vendorId?: string;
    requirementName: string;
    status: number | string;
    statusName?: string;
    expirationDate?: string;
    lastCheckedDate?: string;
    notes?: string;
}

export interface VendorPerformance {
    id?: string;
    vendorId?: string;
    evaluationPeriod: string;
    evaluationDate: string;
    qualityScore: number;
    deliveryScore: number;
    responsivenessScore: number;
    complianceScore: number;
    averageScore?: number;
    comments?: string;
}

export interface VendorRisk {
    id?: string;
    vendorId?: string;
    riskCategory: string;
    riskLevel: number | string;
    riskLevelName?: string;
    riskDescription: string;
    mitigationPlan?: string;
    lastReviewDate: string;
}

export interface VendorDetails extends Vendor {
    contacts: VendorContact[];
    addresses: VendorAddress[];
    bankAccounts: BankAccount[];
    documents: VendorDocument[];
    contracts: VendorContract[];
    compliances: VendorCompliance[];
    performances: VendorPerformance[];
    risks: VendorRisk[];
}

export interface VendorDirectoryItem {
    id: string;
    vendorNumber: string;
    legalName: string;
    tradeName?: string;
    taxRegistrationNumber?: string;
    status: number | string;
    statusName?: string;
    createdAt: string;
    primaryContactName?: string;
    primaryContactEmail?: string;
    primaryContactPhone?: string;
    primaryAddressCity?: string;
    primaryAddressCountry?: string;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

export interface AppUserListItem {
    id: string;
    displayName: string;
    email: string;
    role: string;
    isActive: boolean;
    isDeleted: boolean;
    status: string;
    createdAt: string;
}

export interface CreateUserRequest {
    displayName: string;
    email: string;
    role: string;
    password: string;
}

export interface DashboardSummary {
    totalVendors: number;
    activeVendors: number;
    pendingApprovalVendors: number;
    draftVendors: number;
    terminatedVendors: number;
    totalContractValue: number;
    totalComplianceIssues: number;
    pendingApprovals: any[];
    performanceLeaderboard: any[];
    complianceAlerts: any[];
}
