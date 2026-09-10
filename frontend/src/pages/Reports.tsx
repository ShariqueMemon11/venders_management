import React, { useState } from 'react';
import { FormErrorBanner } from '../components/ui/FormErrorBanner';
import { getApiErrorMessages } from '../utils/apiErrors';

const EXPORT_FALLBACK = 'Report export failed. Try again.';

function reportExportMessages(err: unknown): string[] {
    const msgs = getApiErrorMessages(err).filter((m) => m.trim());
    const unusable = (m: string) =>
        /^(failed to fetch|network error|load failed|download failed\.?)$/i.test(m) ||
        m.startsWith('{') ||
        m.startsWith('<');
    if (!msgs.length || msgs.every(unusable)) return [EXPORT_FALLBACK];
    return msgs;
}

export const Reports: React.FC = () => {
    const userStr = sessionStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    const canMaster = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    const canRisk = user?.role === 'Admin' || user?.role === 'RiskAndCompliance';
    const canPerformance = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    const [formErrors, setFormErrors] = useState<string[] | null>(null);

    const downloadReport = (reportType: string, format: string) => {
        setFormErrors(null);
        const token = sessionStorage.getItem('auth_token');
        const url = `${import.meta.env.VITE_API_BASE_URL}/vendors/reports/${reportType}?format=${format}`;
        // Open with token via temporary fetch+blob for authenticated download
        fetch(url, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
            .then(async (res) => {
                if (!res.ok) {
                    const raw = await res.text();
                    let data: unknown = raw;
                    try {
                        data = JSON.parse(raw);
                    } catch {
                        /* keep plain text */
                    }
                    throw { response: { data, status: res.status }, message: typeof data === 'string' ? data : undefined };
                }
                const blob = await res.blob();
                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                link.download = `${reportType}-report.${format}`;
                link.click();
                URL.revokeObjectURL(link.href);
            })
            .catch((err) => setFormErrors(reportExportMessages(err)));
    };

    return (
        <div className="container-fluid mb-5">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">Analytics & Exports</div>
                    <h2 className="fw-bold mb-0">Reporting Engine</h2>
                    <p className="text-muted mb-0 small">Reports are permission-based by role.</p>
                </div>
            </div>

            <FormErrorBanner messages={formErrors} />

            <div className="row">
                {canMaster && (
                    <div className="col-md-6 col-lg-4 mb-4">
                        <div className="card h-100 shadow-sm border-0">
                            <div className="card-body p-4">
                                <div className="d-flex justify-content-between mb-3">
                                    <div className="bg-primary bg-opacity-10 text-primary rounded p-3">
                                        <i className="bi bi-building fs-4"></i>
                                    </div>
                                </div>
                                <h5 className="fw-bold mb-2">Vendor Master Data</h5>
                                <p className="text-muted small mb-4">Basic vendor profile export (names, status, tax, dates). Procurement & Admin.</p>
                                <div className="d-grid gap-2">
                                    <button className="btn btn-outline-primary text-start" onClick={() => downloadReport("master", "xlsx")}>
                                        <i className="bi bi-file-earmark-excel me-2"></i> Download as Excel (.xlsx)
                                    </button>
                                    <button className="btn btn-outline-secondary text-start" onClick={() => downloadReport("master", "csv")}>
                                        <i className="bi bi-filetype-csv me-2"></i> Download as CSV (.csv)
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {canRisk && (
                    <div className="col-md-6 col-lg-4 mb-4">
                        <div className="card h-100 shadow-sm border-0">
                            <div className="card-body p-4">
                                <div className="d-flex justify-content-between mb-3">
                                    <div className="bg-danger bg-opacity-10 text-danger rounded p-3">
                                        <i className="bi bi-shield-exclamation fs-4"></i>
                                    </div>
                                </div>
                                <h5 className="fw-bold mb-2">Vendor Risk Assessments</h5>
                                <p className="text-muted small mb-4">Risk levels and mitigation plans. Risk & Compliance & Admin.</p>
                                <div className="d-grid gap-2">
                                    <button className="btn btn-outline-danger text-start" onClick={() => downloadReport("risk", "xlsx")}>
                                        <i className="bi bi-file-earmark-excel me-2"></i> Download as Excel (.xlsx)
                                    </button>
                                    <button className="btn btn-outline-secondary text-start" onClick={() => downloadReport("risk", "csv")}>
                                        <i className="bi bi-filetype-csv me-2"></i> Download as CSV (.csv)
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {canPerformance && (
                    <div className="col-md-6 col-lg-4 mb-4">
                        <div className="card h-100 shadow-sm border-0 bg-light opacity-75">
                            <div className="card-body p-4 text-center d-flex flex-column justify-content-center">
                                <i className="bi bi-cone-striped fs-1 text-muted mb-3"></i>
                                <h5 className="fw-bold mb-2">Performance Scorecards</h5>
                                <p className="text-muted small mb-0">Coming soon — Procurement-owned KPI exports.</p>
                            </div>
                        </div>
                    </div>
                )}

                {!canMaster && !canRisk && (
                    <div className="col-12">
                        <div className="alert alert-warning">Your role does not have report download permissions.</div>
                    </div>
                )}
            </div>
        </div>
    );
};
