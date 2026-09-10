import './status-pill.css';

export type StatusTone = 'active' | 'pending' | 'draft' | 'rejected';

type StatusPillProps = {
    status?: number | string;
    label?: string;
};

function compact(value: unknown): string {
    if (value == null) return '';
    return String(value).toLowerCase().replace(/[\s_-]/g, '');
}

/** Existing VendorStatus labels — keep wording; only the tone uses the 4 tokens. */
const VENDOR_STATUS: Record<string, { label: string; tone: StatusTone }> = {
    draft: { label: 'Draft', tone: 'draft' },
    '1': { label: 'Draft', tone: 'draft' },
    pendingreview: { label: 'Pending Review', tone: 'pending' },
    '2': { label: 'Pending Review', tone: 'pending' },
    underverification: { label: 'Under Verification', tone: 'pending' },
    '3': { label: 'Under Verification', tone: 'pending' },
    pendingapproval: { label: 'Pending Approval', tone: 'pending' },
    '4': { label: 'Pending Approval', tone: 'pending' },
    active: { label: 'Active', tone: 'active' },
    '5': { label: 'Active', tone: 'active' },
    suspended: { label: 'Suspended', tone: 'rejected' },
    '6': { label: 'Suspended', tone: 'rejected' },
    inactive: { label: 'Inactive', tone: 'draft' },
    '7': { label: 'Inactive', tone: 'draft' },
    terminated: { label: 'Terminated', tone: 'rejected' },
    '8': { label: 'Terminated', tone: 'rejected' },
    deactivated: { label: 'Deactivated', tone: 'draft' },
};

/** Maps VendorStatus (numeric or name) to the existing display label + token tone. */
export function vendorStatusDisplay(status?: number | string): { label: string; tone: StatusTone } {
    const key = compact(status);
    return VENDOR_STATUS[key] ?? { label: status != null && status !== '' ? String(status) : 'Draft', tone: 'draft' };
}

export function StatusPill({ status, label }: StatusPillProps) {
    const mapped = vendorStatusDisplay(status);
    const text = label ?? mapped.label;
    return (
        <span className={`status-pill status-pill--${mapped.tone}`}>
            <span className="status-pill__dot" aria-hidden />
            {text}
        </span>
    );
}
