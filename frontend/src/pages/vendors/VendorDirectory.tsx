import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { VendorService } from '../../services/vendorService';
import type { VendorDirectoryItem, PagedResult } from '../../types/index';
import { Pagination } from '../../components/ui/Pagination';
import { StatusPill } from '../../components/ui/StatusPill';
import { EmptyState } from '../../components/ui/EmptyState';

const STATUS_OPTIONS: { value: string; label: string }[] = [
    { value: '', label: 'All statuses' },
    { value: '1', label: 'Draft' },
    { value: '2', label: 'Pending Review' },
    { value: '3', label: 'Under Verification' },
    { value: '4', label: 'Pending Approval' },
    { value: '5', label: 'Active' },
    { value: '6', label: 'Suspended' },
    { value: '7', label: 'Inactive' },
    { value: '8', label: 'Terminated' },
];

type DirectoryFilters = { name: string; status: string; country: string };

export const VendorDirectory: React.FC = () => {
    const [data, setData] = useState<PagedResult<VendorDirectoryItem> | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [page, setPage] = useState(1);
    const pageSize = 10;

    const [nameInput, setNameInput] = useState('');
    const [statusInput, setStatusInput] = useState('');
    const [countryInput, setCountryInput] = useState('');
    const [filters, setFilters] = useState<DirectoryFilters>({ name: '', status: '', country: '' });

    const loadData = async (currentPage: number, currentFilters: DirectoryFilters) => {
        setLoading(true);
        try {
            const res = await VendorService.getPagedVendors(currentPage, pageSize, {
                name: currentFilters.name || undefined,
                status: currentFilters.status || undefined,
                country: currentFilters.country || undefined,
            });
            if (res.success || res.Success || Array.isArray(res.data?.items)) {
                const pagedData = res.data || res.Data || res;
                setData(pagedData);
                setError(null);
            } else {
                setError(res.message || res.Message || 'Directory could not be loaded.');
            }
        } catch (err: any) {
            setError(err.message || 'Directory could not be loaded.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadData(page, filters);
    }, [page, filters]);

    const applyFilters = (e?: React.FormEvent) => {
        e?.preventDefault();
        setPage(1);
        setFilters({
            name: nameInput.trim(),
            status: statusInput,
            country: countryInput.trim(),
        });
    };

    const clearFilters = () => {
        setNameInput('');
        setStatusInput('');
        setCountryInput('');
        setPage(1);
        setFilters({ name: '', status: '', country: '' });
    };

    const hasActiveFilters = !!(filters.name || filters.status || filters.country);

    return (
        <div className="container-fluid mb-5">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h2 className="fw-bold mb-0">Vendor Directory</h2>
                    <p className="text-muted mb-0">Detailed view of all vendors and their primary contact information</p>
                </div>
            </div>

            {error && <div className="alert alert-danger">{error}</div>}

            <form className="card shadow-sm border-0 mb-4" onSubmit={applyFilters}>
                <div className="card-body">
                    <div className="row g-3 align-items-end">
                        <div className="col-12 col-lg-4">
                            <label className="form-label small fw-bold mb-1">Name</label>
                            <input
                                type="search"
                                className="form-control"
                                placeholder="Legal name, trade name, or number"
                                value={nameInput}
                                onChange={(e) => setNameInput(e.target.value)}
                            />
                        </div>
                        <div className="col-12 col-lg-3">
                            <label className="form-label small fw-bold mb-1">Status</label>
                            <select
                                className="form-select"
                                value={statusInput}
                                onChange={(e) => setStatusInput(e.target.value)}
                            >
                                {STATUS_OPTIONS.map((opt) => (
                                    <option key={opt.value || 'all'} value={opt.value}>{opt.label}</option>
                                ))}
                            </select>
                        </div>
                        <div className="col-12 col-lg-3">
                            <label className="form-label small fw-bold mb-1">Country</label>
                            <input
                                type="text"
                                className="form-control"
                                placeholder="e.g. United States"
                                value={countryInput}
                                onChange={(e) => setCountryInput(e.target.value)}
                            />
                        </div>
                        <div className="col-12 col-lg-2 d-flex gap-2">
                            <button type="submit" className="btn btn-primary flex-grow-1">
                                <i className="bi bi-search me-1"></i> Search
                            </button>
                            {hasActiveFilters && (
                                <button type="button" className="btn btn-outline-secondary" onClick={clearFilters} title="Clear filters">
                                    <i className="bi bi-x-lg"></i>
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            </form>

            <div className="card shadow-sm border-0 mb-4">
                <div className="card-body p-0">
                    <div className="table-responsive">
                        <table className="table table-hover align-middle mb-0">
                            <thead className="table-light">
                                <tr>
                                    <th className="ps-4">Vendor</th>
                                    <th>Status</th>
                                    <th>Primary Contact</th>
                                    <th>Location</th>
                                    <th>Joined</th>
                                    <th className="text-end pe-4">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {loading && (
                                    <tr>
                                        <td colSpan={6} className="text-center py-5">
                                            <div className="spinner-border text-primary"></div>
                                        </td>
                                    </tr>
                                )}
                                {!loading && data?.items?.length === 0 && (
                                    <tr>
                                        <td colSpan={6}>
                                            <EmptyState
                                                message={hasActiveFilters ? 'No vendors match your filters.' : 'No vendors to show.'}
                                                actionLabel={hasActiveFilters ? 'Clear filters' : undefined}
                                                onAction={hasActiveFilters ? clearFilters : undefined}
                                            />
                                        </td>
                                    </tr>
                                )}
                                {!loading && data?.items?.map(v => (
                                    <tr key={v.id}>
                                        <td className="ps-4">
                                            <div className="fw-bold text-primary">{v.legalName}</div>
                                            <div className="small text-muted font-mono">{v.vendorNumber}</div>
                                        </td>
                                        <td><StatusPill status={v.statusName || v.status} /></td>
                                        <td>
                                            {v.primaryContactName ? (
                                                <>
                                                    <div className="fw-medium">{v.primaryContactName}</div>
                                                    <div className="small text-muted">
                                                        {v.primaryContactEmail && <span><i className="bi bi-envelope"></i> {v.primaryContactEmail}</span>}
                                                        {v.primaryContactEmail && v.primaryContactPhone && <span className="mx-2">|</span>}
                                                        {v.primaryContactPhone && <span><i className="bi bi-telephone"></i> {v.primaryContactPhone}</span>}
                                                    </div>
                                                </>
                                            ) : (
                                                <span className="text-muted fst-italic">No contact</span>
                                            )}
                                        </td>
                                        <td>
                                            {v.primaryAddressCity || v.primaryAddressCountry ? (
                                                <span>
                                                    {v.primaryAddressCity}{v.primaryAddressCity && v.primaryAddressCountry ? ', ' : ''}{v.primaryAddressCountry}
                                                </span>
                                            ) : (
                                                <span className="text-muted fst-italic">No HQ address</span>
                                            )}
                                        </td>
                                        <td className="font-mono">
                                            {new Date(v.createdAt).toLocaleDateString()}
                                        </td>
                                        <td className="text-end pe-4">
                                            <Link to={`/vendors/${v.id}`} className="btn btn-sm btn-outline-primary">
                                                <i className="bi bi-list-check me-1"></i> Details
                                            </Link>
                                        </td>
                                    </tr>
                                ))}
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
