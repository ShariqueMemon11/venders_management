import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { VendorService } from '../../services/vendorService';
import type { Vendor } from '../../types/index';
import { FormErrorBanner } from '../../components/ui/FormErrorBanner';
import { getApiErrorMessages } from '../../utils/apiErrors';

export const VendorCreate: React.FC = () => {
    const navigate = useNavigate();
    const userStr = sessionStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    const isProcurement = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    
    React.useEffect(() => {
        if (!isProcurement) {
            navigate('/vendors/directory');
        }
    }, [isProcurement, navigate]);

    const [vendor, setVendor] = useState<Partial<Vendor>>({
        legalName: '',
        tradeName: '',
        taxRegistrationNumber: '',
        currencyCode: 'USD',
        website: ''
    });
    const [saving, setSaving] = useState(false);
    const [formErrors, setFormErrors] = useState<string[] | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        
        if (!vendor.legalName) {
            setFormErrors(['Legal name is required.']);
            return;
        }

        setSaving(true);
        setFormErrors(null);
        
        try {
            const response = await VendorService.createVendor(vendor);
            if (response.success) {
                const vendorId = response.data;
                navigate(`/vendors/${vendorId}`);
            } else {
                setFormErrors([response.message || 'Draft was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="container mb-5" style={{ maxWidth: '800px' }}>
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">New Supplier</div>
                    <h2>Create Vendor Draft</h2>
                    <p className="text-muted small mb-0">
                        Saves a Draft record only. Add contacts, documents, and bank details on the next page, then click Submit for Approval when ready.
                    </p>
                </div>
                <Link to="/vendors" className="btn btn-outline-secondary">Cancel</Link>
            </div>

            <FormErrorBanner messages={formErrors} />

            <div className="card shadow-sm border-0">
                <div className="card-body p-4">
                    <form onSubmit={handleSubmit}>
                        
                        <div className="mb-3">
                            <label className="form-label fw-bold">Legal Entity Name <span className="text-danger">*</span></label>
                            <input type="text" className="form-control" value={vendor.legalName} onChange={e => setVendor({...vendor, legalName: e.target.value})} required />
                        </div>

                        <div className="mb-3">
                            <label className="form-label fw-bold">Trading / DBA Name</label>
                            <input type="text" className="form-control" value={vendor.tradeName} onChange={e => setVendor({...vendor, tradeName: e.target.value})} />
                        </div>

                        <div className="row">
                            <div className="col-md-6 mb-3">
                                <label className="form-label fw-bold">Tax / VAT Registration No.</label>
                                <input type="text" className="form-control" value={vendor.taxRegistrationNumber} onChange={e => setVendor({...vendor, taxRegistrationNumber: e.target.value})} />
                            </div>

                            <div className="col-md-6 mb-3">
                                <label className="form-label fw-bold">Preferred Currency</label>
                                <input type="text" className="form-control" maxLength={3} value={vendor.currencyCode} onChange={e => setVendor({...vendor, currencyCode: e.target.value})} />
                            </div>
                        </div>

                        <div className="mb-4">
                            <label className="form-label fw-bold">Company Website</label>
                            <input type="text" className="form-control" value={vendor.website} onChange={e => setVendor({...vendor, website: e.target.value})} />
                        </div>

                        <div className="d-flex justify-content-end">
                            <button type="submit" className="btn btn-primary py-2 px-4" disabled={saving}>
                                {saving ? (
                                    <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Saving...</>
                                ) : (
                                    <><i className="bi bi-file-earmark-plus me-2"></i> Save Draft</>
                                )}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
};
