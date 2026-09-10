import { describe, it, expect } from 'vitest';
import { deriveWorkflowDecision, deriveWorkflowStepper } from './WorkflowStepper';

/** Risk approved (status 2) + procurement pending (status 1) — the screenshot bug. */
const procurementTurn = {
    status: 4,
    statusName: 'PendingApproval',
    approvalSteps: [
        {
            stepOrder: 1,
            stepName: 'Risk & Compliance Review',
            status: 2,
            statusName: 'Approved',
            requiredRole: 'RiskAndCompliance',
        },
        {
            stepOrder: 2,
            stepName: 'Procurement Approval',
            status: 1,
            statusName: 'Pending',
            requiredRole: 'ProcurementManager',
        },
    ],
};

describe('deriveWorkflowDecision', () => {
    it('matches the stepper: procurement is current after risk is approved', () => {
        const stepper = deriveWorkflowStepper(4, 'PendingApproval', procurementTurn);
        const decision = deriveWorkflowDecision(4, 'PendingApproval', procurementTurn);

        expect(stepper.visuals.risk).toBe('complete');
        expect(stepper.visuals.procurement).toBe('current');
        expect(decision.currentStepId).toBe('procurement');
        expect(decision.currentStepLabel).toBe('Procurement Approval');
        expect(decision.requiredRole).toBe('ProcurementManager');
        expect(decision.awaitingDecision).toBe(true);
        expect(decision.waitingOnCaption).toBe('Waiting on Procurement Manager.');
    });
});
