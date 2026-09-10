import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { VendorService } from '../services/vendorService';
import type { DashboardSummary } from '../types/index';
import { Permissions, getCurrentUser, hasPermission } from '../auth/permissions';

export const Dashboard: React.FC = () => {
    const [dashboard, setDashboard] = useState<DashboardSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const user = getCurrentUser();
    const role = user?.role || 'Viewer';

    useEffect(() => {
        const loadDashboard = async () => {
            try {
                const response = await VendorService.getDashboardSummary();
                if (response.success) {
                    setDashboard(response.data);
                } else {
                    setError(response.message || 'Dashboard data could not be loaded.');
                }
            } catch (err: any) {
                setError(err.message || 'Dashboard data could not be loaded.');
            } finally {
                setLoading(false);
            }
        };

        loadDashboard();
    }, []);

    if (loading) return <div className="text-center py-5"><div className="spinner-border text-primary"></div></div>;
    if (error) return <div className="alert alert-danger">{error}</div>;
    if (!dashboard) return null;

    const titleByRole: Record<string, string> = {
        Admin: 'Admin Control Center',
        ProcurementManager: 'Procurement Dashboard',
        RiskAndCompliance: 'Risk & Compliance Dashboard',
        Viewer: 'Vendor Directory Overview',
    };

    const showProcurementBlocks = role === 'Admin' || role === 'ProcurementManager';
    const showRiskBlocks = role === 'Admin' || role === 'RiskAndCompliance';
    const showViewerLite = role === 'Viewer';

    return (
        <div className="container-fluid mb-5">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">Role-based workspace · {role}</div>
                    <h2 className="fw-bold mb-0">{titleByRole[role] || 'Enterprise Dashboard'}</h2>
                </div>
                {hasPermission(Permissions.Report.VendorMaster) && (
                    <Link to="/reports" className="btn btn-outline-primary">
                        <i className="bi bi-download me-2"></i>Reports
                    </Link>
                )}
            </div>

            <div className="row g-3">
                <div className="col-xl-3 col-sm-6 mb-3">
                    <div className="card border-0 shadow-sm h-100 border-start border-primary border-4">
                        <div className="card-body">
                            <h6 className="text-muted text-uppercase small fw-bold mb-1">Total Suppliers</h6>
                            <h2 className="display-6 fw-bold mb-0">{dashboard.totalVendors}</h2>
                        </div>
                    </div>
                </div>
                <div className="col-xl-3 col-sm-6 mb-3">
                    <div className="card border-0 shadow-sm h-100 border-start border-success border-4">
                        <div className="card-body">
                            <h6 className="text-muted text-uppercase small fw-bold mb-1">Active Suppliers</h6>
                            <h2 className="display-6 fw-bold mb-0 text-success">{dashboard.activeVendors}</h2>
                        </div>
                    </div>
                </div>
                {(showProcurementBlocks || showRiskBlocks) && (
                    <div className="col-xl-3 col-sm-6 mb-3">
                        <div className="card border-0 shadow-sm h-100 border-start border-warning border-4">
                            <div className="card-body">
                                <h6 className="text-muted text-uppercase small fw-bold mb-1">Pending Approvals</h6>
                                <h2 className="display-6 fw-bold mb-0 text-warning">{dashboard.pendingApprovalVendors}</h2>
                            </div>
                        </div>
                    </div>
                )}
                {showProcurementBlocks && (
                    <div className="col-xl-3 col-sm-6 mb-3">
                        <div className="card border-0 shadow-sm h-100 border-start border-info border-4">
                            <div className="card-body">
                                <h6 className="text-muted text-uppercase small fw-bold mb-1">Draft Vendors</h6>
                                <h2 className="display-6 fw-bold mb-0 text-info">{dashboard.draftVendors}</h2>
                            </div>
                        </div>
                    </div>
                )}
                {showProcurementBlocks && (
                    <div className="col-xl-3 col-sm-6 mb-3">
                        <div className="card border-0 shadow-sm h-100 border-start border-danger border-4">
                            <div className="card-body">
                                <h6 className="text-muted text-uppercase small fw-bold mb-1">Active Contract Value</h6>
                                <h3 className="fw-bold mb-0">${dashboard.totalContractValue.toLocaleString()}</h3>
                            </div>
                        </div>
                    </div>
                )}
                {showRiskBlocks && (
                    <div className="col-xl-3 col-sm-6 mb-3">
                        <div className="card border-0 shadow-sm h-100 border-start border-danger border-4">
                            <div className="card-body">
                                <h6 className="text-muted text-uppercase small fw-bold mb-1">Compliance Issues</h6>
                                <h2 className="display-6 fw-bold mb-0 text-danger">{dashboard.totalComplianceIssues}</h2>
                            </div>
                        </div>
                    </div>
                )}
                {(showProcurementBlocks || role === 'Admin') && (
                    <div className="col-xl-3 col-sm-6 mb-3">
                        <div className="card border-0 shadow-sm h-100 border-start border-secondary border-4">
                            <div className="card-body">
                                <h6 className="text-muted text-uppercase small fw-bold mb-1">Terminated Vendors</h6>
                                <h2 className="display-6 fw-bold mb-0 text-secondary">{dashboard.terminatedVendors ?? 0}</h2>
                            </div>
                        </div>
                    </div>
                )}
            </div>

            {showViewerLite && (
                <div className="alert alert-info mt-3">
                    You have read-only access. Browse the <Link to="/vendors/directory">Vendor Directory</Link> for basic profiles and public contacts.
                </div>
            )}

            {(showProcurementBlocks || showRiskBlocks) && (
                <div className="row mt-4">
                    <div className="col-12 mb-4">
                        <div className="card shadow-sm border-0 border-top border-warning border-4">
                            <div className="card-header bg-white py-3">
                                <h5 className="mb-0 fw-bold">
                                    <i className="bi bi-list-check text-warning me-2"></i>
                                    {showRiskBlocks && !showProcurementBlocks ? 'My Review Queue' : 'Pending Approvals'} ({dashboard.pendingApprovals.length})
                                </h5>
                            </div>
                            <div className="card-body p-0">
                                {dashboard.pendingApprovals.length === 0 ? (
                                    <div className="text-center p-4 text-muted">No pending workflow requests.</div>
                                ) : (
                                    <div className="table-responsive">
                                        <table className="table table-hover align-middle mb-0">
                                            <thead className="bg-light">
                                                <tr>
                                                    <th className="ps-4">Request / Vendor</th>
                                                    <th>Type</th>
                                                    <th>Submitted</th>
                                                    <th>Details</th>
                                                    <th className="text-end pe-4">Action</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {dashboard.pendingApprovals.map((req: any, idx: number) => (
                                                    <tr key={idx}>
                                                        <td className="ps-4 fw-bold">{req.itemName}</td>
                                                        <td><span className="badge bg-secondary">{req.type}</span></td>
                                                        <td className="small font-mono">{new Date(req.submittedAt).toLocaleDateString()}</td>
                                                        <td className="text-muted small">{req.detail}</td>
                                                        <td className="text-end pe-4">
                                                            <Link to={`/vendors/${req.id}`} className="btn btn-sm btn-outline-primary">Review</Link>
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>

                    {showRiskBlocks && (
                        <div className="col-md-6 mb-4">
                            <div className="card shadow-sm border-0 h-100">
                                <div className="card-header bg-white py-3">
                                    <h5 className="mb-0 fw-bold"><i className="bi bi-shield-exclamation text-danger me-2"></i>Compliance Alerts</h5>
                                </div>
                                <div className="card-body p-0">
                                    {dashboard.complianceAlerts.length === 0 ? (
                                        <div className="text-center p-4 text-muted">No active risk alerts.</div>
                                    ) : (
                                        <ul className="list-group list-group-flush">
                                            {dashboard.complianceAlerts.map((alert: any, idx: number) => (
                                                <li key={idx} className="list-group-item px-4 py-3 d-flex justify-content-between">
                                                    <div>
                                                        <h6 className="mb-1">{alert.vendorName}</h6>
                                                        <span className="small text-muted">{alert.requirementName}</span>
                                                    </div>
                                                    <span className={`badge ${alert.severity === 'Critical' ? 'bg-danger' : 'bg-warning'}`}>{alert.severity}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </div>
                            </div>
                        </div>
                    )}

                    {showProcurementBlocks && (
                        <div className="col-md-6 mb-4">
                            <div className="card shadow-sm border-0 h-100">
                                <div className="card-header bg-white py-3">
                                    <h5 className="mb-0 fw-bold"><i className="bi bi-trophy text-warning me-2"></i>Top Performers</h5>
                                </div>
                                <div className="card-body p-0">
                                    {dashboard.performanceLeaderboard.length === 0 ? (
                                        <div className="text-center p-4 text-muted">No performance data recorded yet.</div>
                                    ) : (
                                        <ul className="list-group list-group-flush">
                                            {dashboard.performanceLeaderboard.map((vendor: any, idx: number) => (
                                                <li key={idx} className="list-group-item px-4 py-3 d-flex justify-content-between align-items-center">
                                                    <div>
                                                        <span className="text-muted small me-2">#{idx + 1}</span>
                                                        <strong>{vendor.vendorName}</strong>
                                                    </div>
                                                    <span className="badge bg-primary rounded-pill">{vendor.averageScore.toFixed(1)} / 100</span>
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            )}

            {role === 'Admin' && (
                <div className="card shadow-sm border-0 mt-2">
                    <div className="card-body">
                        <h5 className="fw-bold mb-2"><i className="bi bi-gear me-2"></i>Admin shortcuts</h5>
                        <div className="d-flex flex-wrap gap-2">
                            <Link className="btn btn-sm btn-outline-dark" to="/vendors">Suppliers</Link>
                            <Link className="btn btn-sm btn-outline-dark" to="/reports">All Reports</Link>
                            <Link className="btn btn-sm btn-outline-dark" to="/vendors/directory">Directory</Link>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};
