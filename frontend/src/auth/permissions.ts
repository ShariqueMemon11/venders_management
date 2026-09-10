/** Frontend mirror of Shared.Application.Authorization.AppPermissions */
export const Permissions = {
  Vendor: {
    View: 'Vendor.View',
    Create: 'Vendor.Create',
    Edit: 'Vendor.Edit',
    Submit: 'Vendor.Submit',
    Terminate: 'Vendor.Terminate',
    ViewSensitive: 'Vendor.ViewSensitive',
  },
  Workflow: {
    View: 'Workflow.View',
    Approve: 'Workflow.Approve',
    Reject: 'Workflow.Reject',
    Start: 'Workflow.Start',
  },
  Compliance: { View: 'Compliance.View', Edit: 'Compliance.Edit' },
  Risk: { View: 'Risk.View', Edit: 'Risk.Edit' },
  Performance: { View: 'Performance.View', Edit: 'Performance.Edit' },
  Document: {
    Upload: 'Document.Upload',
    Download: 'Document.Download',
    Delete: 'Document.Delete',
    Verify: 'Document.Verify',
  },
  Contract: { View: 'Contract.View', Edit: 'Contract.Edit' },
  Report: {
    VendorMaster: 'Report.VendorMaster',
    Risk: 'Report.Risk',
    Performance: 'Report.Performance',
  },
  User: {
    Manage: 'User.Manage',
  },
} as const;

export type AppUser = {
  email?: string;
  name?: string;
  role?: string;
  permissions?: string[];
};

export function getCurrentUser(): AppUser | null {
  const raw = sessionStorage.getItem('user');
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function hasPermission(permission: string): boolean {
  const user = getCurrentUser();
  return !!user?.permissions?.includes(permission);
}

export function hasAnyPermission(...permissions: string[]): boolean {
  return permissions.some((p) => hasPermission(p));
}
