import React, { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { VendorService } from '../../services/vendorService';
import type { VendorDetails } from '../../types/index';
import { useModal } from '../../components/ui/ModalContext';
import { FormErrorBanner } from '../../components/ui/FormErrorBanner';
import { getApiErrorMessages } from '../../utils/apiErrors';

export const VendorEdit: React.FC = () => {
    const { id } = useParams<{id: string}>();
    const navigate = useNavigate();
    const { showConfirm } = useModal();
    
    const userStr = sessionStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    const isProcurement = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    const isAdmin = user?.role === 'Admin';

    useEffect(() => {
        if (!isProcurement) {
            navigate(`/vendors/${id}`);
        }
    }, [isProcurement, navigate, id]);
    
    const [vendor, setVendor] = useState<Partial<VendorDetails>>({});
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [formErrors, setFormErrors] = useState<string[] | null>(null);

    useEffect(() => {
        if (!id) return;
        const loadVendor = async () => {
            try {
                const response = await VendorService.getVendorById(id);
                if (response.success) {
                    setVendor(response.data);
                } else {
                setFormErrors([response.message || 'Vendor was not found.']);
                }
            } catch (err: unknown) {
                setFormErrors(getApiErrorMessages(err));
            } finally {
                setLoading(false);
            }
        };
        loadVendor();
    }, [id]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!vendor.legalName) {
            setFormErrors(['Legal name is required.']);
            return;
        }
        
        setSaving(true);
        setFormErrors(null);
        try {
            const response = await VendorService.updateVendor(id!, vendor);
            if (response.success) {
                navigate(`/vendors/${id}`);
            } else {
                setFormErrors([response.message || 'Changes were not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleTerminate = async () => {
        const confirmed = await showConfirm(
            "This will terminate the vendor and keep the record for audit history. Continue?",
            "Terminate Vendor"
        );
        if (confirmed) {
            setSaving(true);
            try {
                await VendorService.terminateVendor(id!);
                navigate('/vendors');
            } catch (err: unknown) {
                setFormErrors(getApiErrorMessages(err));
                setSaving(false);
            }
        }
    };

    if (loading) return <div className="text-center py-5"><div className="spinner-border text-primary"></div></div>;

    return (
        <div className="container mb-5" style={{ maxWidth: '800px' }}>
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">Edit Onboarded Record</div>
                    <h2>Update Vendor Profile</h2>
                </div>
                <Link to={`/vendors/${id}`} className="btn btn-outline-secondary">Cancel</Link>
            </div>

            <FormErrorBanner messages={formErrors} />

            <div className="card shadow-sm border-0">
                <div className="card-body p-4">
                    <form onSubmit={handleSubmit}>
                        
                        <div className="row">
                            <div className="col-md-6 mb-3">
                                <label className="form-label fw-bold text-muted">Vendor Number (Read-only)</label>
                                <input type="text" className="form-control bg-light font-mono" value={vendor.vendorNumber || 'Pending Generation'} readOnly />
                            </div>
                            
                            <div className="col-md-6 mb-3">
                                <label className="form-label fw-bold">Lifecycle Status</label>
                                <select className="form-select" value={vendor.status} onChange={e => setVendor({...vendor, status: parseInt(e.target.value)})}>
                                    <option value="1">Draft</option>
                                    <option value="2" disabled>Pending Review (Workflow)</option>
                                    <option value="3" disabled>Under Verification (Workflow)</option>
                                    <option value="4" disabled>Pending Approval (Workflow)</option>
                                    <option value="5">Active</option>
                                    <option value="6">Suspended</option>
                                    <option value="7">Inactive</option>
                                    <option value="8">Terminated</option>
                                </select>
                            </div>
                        </div>

                        <div className="mb-3">
                            <label className="form-label fw-bold">Legal Entity Name <span className="text-danger">*</span></label>
                            <input type="text" className="form-control" value={vendor.legalName || ''} onChange={e => setVendor({...vendor, legalName: e.target.value})} required />
                        </div>

                        <div className="mb-3">
                            <label className="form-label fw-bold">Trading / DBA Name</label>
                            <input type="text" className="form-control" value={vendor.tradeName || ''} onChange={e => setVendor({...vendor, tradeName: e.target.value})} />
                        </div>

                        <div className="row">
                            <div className="col-md-6 mb-3">
                                <label className="form-label fw-bold">Tax / VAT Registration No.</label>
                                <input type="text" className="form-control" value={vendor.taxRegistrationNumber || ''} onChange={e => setVendor({...vendor, taxRegistrationNumber: e.target.value})} />
                            </div>
                            <div className="col-md-6 mb-3">
                                <label className="form-label fw-bold">Preferred Currency</label>
                                <input type="text" className="form-control" maxLength={3} value={vendor.currencyCode || ''} onChange={e => setVendor({...vendor, currencyCode: e.target.value})} />
                            </div>
                        </div>

                        <div className="mb-4">
                            <label className="form-label fw-bold">Company Website</label>
                            <input type="text" className="form-control" value={vendor.website || ''} onChange={e => setVendor({...vendor, website: e.target.value})} />
                        </div>

                        <div className="d-flex justify-content-between align-items-center">
                            {isAdmin ? (
                                <button type="button" className="btn btn-outline-danger" onClick={handleTerminate} disabled={saving}>
                                    <i className="bi bi-slash-circle me-1"></i> Terminate Vendor
                                </button>
                            ) : <span />}
                            
                            <button type="submit" className="btn btn-primary py-2 px-4" disabled={saving}>
                                {saving ? 'Updating...' : 'Save Changes'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
};
