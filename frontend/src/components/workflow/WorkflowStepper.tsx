import './workflow-stepper.css';

export const WORKFLOW_STEP_IDS = ['draft', 'submitted', 'risk', 'procurement', 'active'] as const;
export type WorkflowStepId = (typeof WORKFLOW_STEP_IDS)[number];

export const WORKFLOW_STEP_LABELS: Record<WorkflowStepId, string> = {
    draft: 'Draft',
    submitted: 'Submitted',
    risk: 'Risk Review',
    procurement: 'Procurement Approval',
    active: 'Active',
};

export type WorkflowStepVisual = 'complete' | 'current' | 'upcoming' | 'rejected';

export type WorkflowSnapshot = {
    status?: number | string;
    statusName?: string;
    rejectionReason?: string;
    approvalSteps?: Array<{
        stepOrder?: number;
        stepName?: string;
        status?: number | string;
        statusName?: string;
        comments?: string;
        requiredRole?: string;
    }>;
};

type DerivedStepper = {
    visuals: Record<WorkflowStepId, WorkflowStepVisual>;
    rejectedAt: WorkflowStepId | null;
    returnedToDraft: boolean;
    rejectionReason: string | null;
};

function compact(value: unknown): string {
    if (value == null) return '';
    return String(value).toLowerCase().replace(/[\s_-]/g, '');
}

function vendorKey(status?: number | string, statusName?: string): string {
    const named = compact(statusName);
    if (named) return named;
    if (typeof status === 'number') {
        const map: Record<number, string> = {
            1: 'draft',
            2: 'pendingreview',
            3: 'underverification',
            4: 'pendingapproval',
            5: 'active',
            6: 'suspended',
            7: 'inactive',
            8: 'terminated',
        };
        return map[status] ?? '';
    }
    return compact(status);
}

function workflowKey(workflow?: WorkflowSnapshot | null): string {
    const named = compact(workflow?.statusName);
    if (named) return named;
    if (typeof workflow?.status === 'number') {
        const map: Record<number, string> = {
            1: 'draft',
            2: 'submitted',
            3: 'underreview',
            4: 'pendingapproval',
            5: 'approved',
            6: 'rejected',
            7: 'cancelled',
        };
        return map[workflow.status] ?? '';
    }
    return compact(workflow?.status);
}

function stepKey(status?: number | string, statusName?: string): string {
    const named = compact(statusName);
    if (named) return named;
    if (typeof status === 'number') {
        const map: Record<number, string> = {
            1: 'pending',
            2: 'approved',
            3: 'rejected',
        };
        return map[status] ?? '';
    }
    return compact(status);
}

type ApprovalStep = NonNullable<WorkflowSnapshot['approvalSteps']>[number];

function isApproved(step?: ApprovalStep): boolean {
    const key = stepKey(step?.status, step?.statusName);
    return key === 'approved';
}

function isRejected(step?: ApprovalStep): boolean {
    const key = stepKey(step?.status, step?.statusName);
    return key === 'rejected';
}

function isPending(step?: ApprovalStep): boolean {
    const key = stepKey(step?.status, step?.statusName);
    return key === 'pending' || key === 'inprogress';
}

function emptyVisuals(fill: WorkflowStepVisual): Record<WorkflowStepId, WorkflowStepVisual> {
    return {
        draft: fill,
        submitted: fill,
        risk: fill,
        procurement: fill,
        active: fill,
    };
}

function markThrough(current: WorkflowStepId): Record<WorkflowStepId, WorkflowStepVisual> {
    const visuals = emptyVisuals('upcoming');
    const idx = WORKFLOW_STEP_IDS.indexOf(current);
    WORKFLOW_STEP_IDS.forEach((id, i) => {
        if (i < idx) visuals[id] = 'complete';
        else if (i === idx) visuals[id] = 'current';
        else visuals[id] = 'upcoming';
    });
    return visuals;
}

/** Pure mapping from vendor Status + workflow CurrentState / approval steps. */
export function deriveWorkflowStepper(
    vendorStatus?: number | string,
    vendorStatusName?: string,
    workflow?: WorkflowSnapshot | null
): DerivedStepper {
    const vendor = vendorKey(vendorStatus, vendorStatusName);
    const wf = workflowKey(workflow);
    const steps = [...(workflow?.approvalSteps ?? [])].sort(
        (a, b) => (a.stepOrder ?? 0) - (b.stepOrder ?? 0)
    );
    const risk = steps.find((s) => s.stepOrder === 1) ?? steps[0];
    const procurement = steps.find((s) => s.stepOrder === 2) ?? steps[1];
    const rejectedStep = steps.find(isRejected);
    const reason =
        workflow?.rejectionReason?.trim() ||
        rejectedStep?.comments?.trim() ||
        null;

    if (wf === 'rejected' || rejectedStep) {
        const rejectedAt: WorkflowStepId =
            rejectedStep && (rejectedStep.stepOrder ?? 0) >= 2 ? 'procurement' : 'risk';
        const visuals = emptyVisuals('upcoming');
        visuals.draft = 'current';
        visuals.submitted = 'complete';
        visuals.risk = rejectedAt === 'risk' ? 'rejected' : 'complete';
        visuals.procurement = rejectedAt === 'procurement' ? 'rejected' : 'upcoming';
        visuals.active = 'upcoming';
        return {
            visuals,
            rejectedAt,
            returnedToDraft: true,
            rejectionReason: reason,
        };
    }

    if (
        vendor === 'active' ||
        vendor === 'suspended' ||
        vendor === 'inactive' ||
        vendor === 'terminated' ||
        wf === 'approved'
    ) {
        const visuals = emptyVisuals('complete');
        visuals.active = 'current';
        return { visuals, rejectedAt: null, returnedToDraft: false, rejectionReason: null };
    }

    if (vendor === 'draft' || vendor === '') {
        return {
            visuals: markThrough('draft'),
            rejectedAt: null,
            returnedToDraft: false,
            rejectionReason: null,
        };
    }

    if (risk && isPending(risk) && !isApproved(risk)) {
        return {
            visuals: markThrough('risk'),
            rejectedAt: null,
            returnedToDraft: false,
            rejectionReason: null,
        };
    }

    if (risk && isApproved(risk) && (!procurement || isPending(procurement))) {
        return {
            visuals: markThrough('procurement'),
            rejectedAt: null,
            returnedToDraft: false,
            rejectionReason: null,
        };
    }

    if (vendor === 'pendingapproval') {
        return {
            visuals: markThrough('procurement'),
            rejectedAt: null,
            returnedToDraft: false,
            rejectionReason: null,
        };
    }

    if (vendor === 'pendingreview' || vendor === 'underverification' || wf === 'underreview' || wf === 'submitted') {
        return {
            visuals: markThrough('risk'),
            rejectedAt: null,
            returnedToDraft: false,
            rejectionReason: null,
        };
    }

    return {
        visuals: markThrough('submitted'),
        rejectedAt: null,
        returnedToDraft: false,
        rejectionReason: null,
    };
}

const ROLE_DISPLAY: Record<string, string> = {
    ProcurementManager: 'Procurement Manager',
    RiskAndCompliance: 'Risk & Compliance',
    Admin: 'Admin',
};

export type WorkflowDecision = {
    currentStepId: WorkflowStepId;
    currentStepLabel: string;
    awaitingDecision: boolean;
    requiredRole: string | null;
    waitingOnCaption: string;
};

/** Same mapping the stepper uses: current step from visuals, role from that step's assignment. */
export function deriveWorkflowDecision(
    vendorStatus?: number | string,
    vendorStatusName?: string,
    workflow?: WorkflowSnapshot | null
): WorkflowDecision {
    const derived = deriveWorkflowStepper(vendorStatus, vendorStatusName, workflow);
    const currentStepId =
        WORKFLOW_STEP_IDS.find((id) => derived.visuals[id] === 'current') ?? 'draft';
    const awaitingDecision = currentStepId === 'risk' || currentStepId === 'procurement';

    const steps = [...(workflow?.approvalSteps ?? [])].sort(
        (a, b) => (a.stepOrder ?? 0) - (b.stepOrder ?? 0)
    );
    const order = currentStepId === 'risk' ? 1 : currentStepId === 'procurement' ? 2 : null;
    const step =
        order == null
            ? undefined
            : (steps.find((s) => s.stepOrder === order) ?? (order === 1 ? steps[0] : steps[1]));

    const fallbackRole =
        currentStepId === 'risk'
            ? 'RiskAndCompliance'
            : currentStepId === 'procurement'
              ? 'ProcurementManager'
              : null;
    const requiredRole = awaitingDecision
        ? (step?.requiredRole?.trim() || fallbackRole)
        : null;
    const pretty = requiredRole ? (ROLE_DISPLAY[requiredRole] ?? requiredRole) : 'the assigned approver';

    return {
        currentStepId,
        currentStepLabel: WORKFLOW_STEP_LABELS[currentStepId],
        awaitingDecision,
        requiredRole,
        waitingOnCaption: `Waiting on ${pretty}.`,
    };
}

function MarkerIcon({ visual }: { visual: WorkflowStepVisual }) {
    if (visual === 'complete') return <i className="bi bi-check-lg" aria-hidden />;
    if (visual === 'rejected') return <i className="bi bi-x-lg" aria-hidden />;
    return null;
}

type WorkflowStepperProps = {
    vendorStatus?: number | string;
    vendorStatusName?: string;
    workflow?: WorkflowSnapshot | null;
};

export function WorkflowStepper({ vendorStatus, vendorStatusName, workflow }: WorkflowStepperProps) {
    const derived = deriveWorkflowStepper(vendorStatus, vendorStatusName, workflow);

    return (
        <div className="workflow-stepper" role="list" aria-label="Vendor approval workflow">
            <ol className="workflow-stepper__track">
                {WORKFLOW_STEP_IDS.map((id, index) => {
                    const visual = derived.visuals[id];
                    const prev = index > 0 ? WORKFLOW_STEP_IDS[index - 1] : null;
                    const prevVisual = prev ? derived.visuals[prev] : null;
                    const connector =
                        prevVisual === 'rejected' || visual === 'rejected'
                            ? 'rejected'
                            : prevVisual === 'complete' || prevVisual === 'current'
                              ? 'complete'
                              : 'upcoming';

                    return (
                        <li
                            key={id}
                            className={`workflow-stepper__item workflow-stepper__item--${visual}`}
                            role="listitem"
                        >
                            {index > 0 && (
                                <span
                                    className={`workflow-stepper__connector workflow-stepper__connector--${connector}`}
                                    aria-hidden
                                />
                            )}
                            <span className={`workflow-stepper__marker workflow-stepper__marker--${visual}`}>
                                <MarkerIcon visual={visual} />
                            </span>
                            <span className="workflow-stepper__label">{WORKFLOW_STEP_LABELS[id]}</span>
                            {derived.rejectedAt === id && (
                                <div className="workflow-stepper__branch">
                                    <span className="workflow-stepper__branch-line" aria-hidden />
                                    <span className="workflow-stepper__branch-label">Returned to Draft</span>
                                </div>
                            )}
                        </li>
                    );
                })}
            </ol>
            {derived.returnedToDraft && (
                <p
                    className="workflow-stepper__caption"
                    title={derived.rejectionReason || 'Vendor was sent back to Draft'}
                >
                    Sent back to Draft
                    {derived.rejectionReason ? ` — ${derived.rejectionReason}` : ' with a rejection from the current review step.'}
                </p>
            )}
        </div>
    );
}
