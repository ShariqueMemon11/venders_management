import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { VendorService } from '../../services/vendorService';
import type { VendorDirectoryItem, PagedResult } from '../../types/index';
import { Pagination } from '../../components/ui/Pagination';
import { StatusPill } from '../../components/ui/StatusPill';
import { EmptyState } from '../../components/ui/EmptyState';

export const VendorList: React.FC = () => {
    const [data, setData] = useState<PagedResult<VendorDirectoryItem> | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [page, setPage] = useState(1);
    const pageSize = 10;

    useEffect(() => {
        const loadVendors = async () => {
            setLoading(true);
            try {
                const response = await VendorService.getPagedVendors(page, pageSize);
                if (response.success) {
                    setData(response.data);
                    setError(null);
                } else {
                    setError(response.message || 'Vendors could not be loaded.');
                }
            } catch (err: any) {
                setError(err.message || 'Vendors could not be loaded.');
            } finally {
                setLoading(false);
            }
        };

        loadVendors();
    }, [page]);

    const userStr = sessionStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    const isProcurement = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    const vendors = data?.items ?? [];

    return (
        <div className="container-fluid mb-5">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">Vendor Repository</div>
                    <h2 className="fw-bold mb-0">Supplier Directory</h2>
                </div>
                <div>
                    {isProcurement && (
                        <Link to="/vendors/new" className="btn btn-primary shadow-sm">
                            <i className="bi bi-plus-lg me-2"></i> Onboard New Vendor
                        </Link>
                    )}
                </div>
            </div>

            {error && <div className="alert alert-danger">{error}</div>}

            <div className="card shadow-sm border-0">
                <div className="card-body p-0">
                    <div className="table-responsive">
                        <table className="table table-hover align-middle mb-0">
                            <thead className="bg-light">
                                <tr>
                                    <th className="ps-4">Number</th>
                                    <th>Legal Name</th>
                                    <th>Trading As</th>
                                    <th>Tax Reg.</th>
                                    <th>Status</th>
                                    <th>Added On</th>
                                    <th className="text-end pe-4">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {loading ? (
                                    <tr>
                                        <td colSpan={7} className="text-center py-5">
                                            <div className="spinner-border text-primary"></div>
                                        </td>
                                    </tr>
                                ) : vendors.length === 0 ? (
                                    <tr>
                                        <td colSpan={7}>
                                            <EmptyState message="No vendors to show." />
                                        </td>
                                    </tr>
                                ) : (
                                    vendors.map(v => (
                                        <tr key={v.id}>
                                            <td className="ps-4 text-muted small font-mono">{v.vendorNumber || 'Draft'}</td>
                                            <td className="fw-bold text-dark">{v.legalName}</td>
                                            <td>{v.tradeName || '-'}</td>
                                            <td>{v.taxRegistrationNumber || '-'}</td>
                                            <td><StatusPill status={v.statusName || v.status} /></td>
                                            <td className="small font-mono">{v.createdAt ? new Date(v.createdAt).toLocaleDateString() : 'Just now'}</td>
                                            <td className="text-end pe-4">
                                                <Link to={`/vendors/${v.id}`} className="btn btn-sm btn-outline-primary">
                                                    Manage
                                                </Link>
                                            </td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
                {data && data.totalPages > 1 && (
                    <div className="card-footer bg-white py-3">
                        <Pagination
                            currentPage={data.pageNumber}
                            totalPages={data.totalPages}
                            onPageChange={(p) => setPage(p)}
                        />
                    </div>
                )}
            </div>
        </div>
    );
};
