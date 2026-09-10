import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { VendorService } from '../../services/vendorService';
import type { VendorDetails, VendorContact, VendorAddress, BankAccount, VendorContract, VendorPerformance, VendorRisk, VendorCompliance, VendorDocument } from '../../types/index';
import { useModal } from '../../components/ui/ModalContext';
import { Permissions, hasPermission } from '../../auth/permissions';
import { FormErrorBanner } from '../../components/ui/FormErrorBanner';
import { WorkflowStepper, deriveWorkflowDecision } from '../../components/workflow/WorkflowStepper';
import { StatusPill } from '../../components/ui/StatusPill';
import { getApiErrorMessages } from '../../utils/apiErrors';

export const VendorDetailsView: React.FC = () => {
    const { id } = useParams<{id: string}>();
    const { showAlert, showConfirm, showPrompt } = useModal();
    const [vendor, setVendor] = useState<VendorDetails | null>(null);
    const [activeRequest, setActiveRequest] = useState<any | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [activeTab, setActiveTab] = useState('overview');
    const [downloadingDocId, setDownloadingDocId] = useState<string | null>(null);
    const [verifyingDocId, setVerifyingDocId] = useState<string | null>(null);
    const [formErrors, setFormErrors] = useState<string[] | null>(null);

    const userStr = sessionStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    const isProcurement = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    const isRiskAndCompliance = user?.role === 'Admin' || user?.role === 'RiskAndCompliance';
    const isAdmin = user?.role === 'Admin';
    const isViewer = user?.role === 'Viewer';
    const canDownloadDocument = hasPermission(Permissions.Document.Download);
    const canVerifyDocument = hasPermission(Permissions.Document.Verify);

    // Current step + required role come only from deriveWorkflowStepper (same mapping as the UI stepper).
    const workflowDecision = deriveWorkflowDecision(vendor?.status, vendor?.statusName, activeRequest);
    const canActOnCurrentStep =
        workflowDecision.awaitingDecision &&
        (isAdmin ||
            (!!workflowDecision.requiredRole &&
                (workflowDecision.requiredRole === user?.role ||
                    workflowDecision.requiredRole === user?.email)));

    const allTabs = ['overview', 'contacts', 'addresses', 'banking', 'documents', 'contracts', 'compliance', 'performance', 'risk'];
    const viewerTabs = ['overview', 'contacts', 'addresses'];
    const visibleTabs = isViewer ? viewerTabs : allTabs;

    // Modals state
    const emptyContact = (): Partial<VendorContact> => ({ isActive: true, isPrimary: false, contactType: 1 });
    const emptyBank = (): Partial<BankAccount> => ({ isVerified: false, isApproved: false, currencyCode: 'USD' });

    const [showContactModal, setShowContactModal] = useState(false);
    const [editingContactId, setEditingContactId] = useState<string | null>(null);
    const [newContact, setNewContact] = useState<Partial<VendorContact>>(emptyContact());
    
    const [showAddressModal, setShowAddressModal] = useState(false);
    const [newAddress, setNewAddress] = useState<Partial<VendorAddress>>({ addressType: 1 });

    const [showBankModal, setShowBankModal] = useState(false);
    const [editingBankId, setEditingBankId] = useState<string | null>(null);
    const [newBank, setNewBank] = useState<Partial<BankAccount>>(emptyBank());

    const [showDocumentModal, setShowDocumentModal] = useState(false);
    const [newDocument, setNewDocument] = useState<{ title: string, type: number, expiryDate: string, file: File | null }>({ title: '', type: 1, expiryDate: '', file: null });
    const [uploading, setUploading] = useState(false);

    const [showContractModal, setShowContractModal] = useState(false);
    const [newContract, setNewContract] = useState<Partial<VendorContract>>({ status: 1 });

    const [showComplianceModal, setShowComplianceModal] = useState(false);
    const [newCompliance, setNewCompliance] = useState<Partial<VendorCompliance>>({ status: 1 });

    const [showPerformanceModal, setShowPerformanceModal] = useState(false);
    const [newPerformance, setNewPerformance] = useState<Partial<VendorPerformance>>({ 
        qualityScore: 100, 
        deliveryScore: 100, 
        responsivenessScore: 100, 
        complianceScore: 100 
    });

    const [showRiskModal, setShowRiskModal] = useState(false);
    const [newRisk, setNewRisk] = useState<Partial<VendorRisk>>({ riskLevel: 1 });

    const [saving, setSaving] = useState(false);

    const loadVendor = async () => {
        setLoading(true);
        try {
            const response = await VendorService.getVendorById(id!);
            if (response.success) {
                setVendor(response.data);
                
                // always check for active workflow request in case of manual status change
                try {
                    const reqResponse = await VendorService.getActiveRequest(id!);
                    if (reqResponse.success || reqResponse.Success) {
                        setActiveRequest(reqResponse.data || reqResponse.Data);
                    } else {
                        setActiveRequest(null);
                    }
                } catch (e) {
                    // ignore if no active request
                    setActiveRequest(null);
                }

            } else {
                setError(response.message || 'Vendor could not be loaded.');
            }
        } catch (err: unknown) {
            setError(getApiErrorMessages(err).join(' '));
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadVendor();
    }, [id]);

    const openCreateContact = () => {
        setEditingContactId(null);
        setNewContact(emptyContact());
        setFormErrors(null);
        setShowContactModal(true);
    };

    const openEditContact = (c: VendorContact) => {
        setEditingContactId(c.id!);
        setNewContact({
            id: c.id,
            vendorId: c.vendorId,
            name: c.name,
            email: c.email,
            jobTitle: c.jobTitle,
            phone: c.phone,
            mobile: c.mobile,
            contactType: typeof c.contactType === 'string' ? c.contactType : c.contactType,
            isPrimary: c.isPrimary,
            isActive: c.isActive,
        });
        setFormErrors(null);
        setShowContactModal(true);
    };

    const closeContactModal = () => {
        setShowContactModal(false);
        setEditingContactId(null);
        setNewContact(emptyContact());
        setFormErrors(null);
    };

    const handleSaveContact = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const payload = {
                ...newContact,
                vendorId: id,
                contactType: typeof newContact.contactType === 'string'
                    ? newContact.contactType
                    : Number(newContact.contactType ?? 1),
            };
            const res = editingContactId
                ? await VendorService.updateContact(editingContactId, { ...payload, id: editingContactId })
                : await VendorService.addContact(id!, payload);
            if (res.success) {
                closeContactModal();
                loadVendor();
                await showAlert(editingContactId ? 'Contact updated.' : 'Contact saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Contact was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleSaveAddress = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const res = await VendorService.addAddress(id!, newAddress);
            if (res.success) {
                setShowAddressModal(false);
                setNewAddress({ addressType: 1 });
                setFormErrors(null);
                loadVendor();
                await showAlert('Address saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Address was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const openCreateBank = () => {
        setEditingBankId(null);
        setNewBank(emptyBank());
        setFormErrors(null);
        setShowBankModal(true);
    };

    const openEditBank = (b: BankAccount) => {
        setEditingBankId(b.id!);
        setNewBank({
            id: b.id,
            vendorId: b.vendorId,
            bankName: b.bankName,
            accountName: b.accountName,
            accountNumber: b.accountNumber,
            iban: b.iban,
            swiftBic: b.swiftBic,
            currencyCode: b.currencyCode || 'USD',
            isVerified: b.isVerified ?? false,
            isApproved: b.isApproved ?? false,
        });
        setFormErrors(null);
        setShowBankModal(true);
    };

    const closeBankModal = () => {
        setShowBankModal(false);
        setEditingBankId(null);
        setNewBank(emptyBank());
        setFormErrors(null);
    };

    const handleSaveBank = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const payload = { ...newBank, vendorId: id };
            const res = editingBankId
                ? await VendorService.updateBankAccount(editingBankId, { ...payload, id: editingBankId })
                : await VendorService.addBankAccount(id!, payload);
            if (res.success) {
                closeBankModal();
                loadVendor();
                await showAlert(editingBankId ? 'Account updated.' : 'Account saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Bank account was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleUploadDocument = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newDocument.file) {
            await showAlert('Select a file to upload.', 'Warning');
            return;
        }

        setUploading(true);
        setFormErrors(null);
        try {
            const formData = new FormData();
            formData.append('title', newDocument.title);
            formData.append('type', newDocument.type.toString());
            if (newDocument.expiryDate) {
                formData.append('expiryDate', newDocument.expiryDate);
            }
            formData.append('file', newDocument.file);

            const res = await VendorService.uploadDocument(id!, formData);
            if (res.success) {
                setShowDocumentModal(false);
                setNewDocument({ title: '', type: 1, expiryDate: '', file: null });
                setFormErrors(null);
                loadVendor();
                await showAlert('Document uploaded.', 'Success');
            } else {
                setFormErrors([res.message || 'Document was not uploaded.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setUploading(false);
        }
    };

    const handleSaveContract = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const res = await VendorService.addContract(id!, newContract);
            if (res.success) {
                setShowContractModal(false);
                setNewContract({ status: 1 });
                setFormErrors(null);
                loadVendor();
                await showAlert('Contract saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Contract was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleSaveCompliance = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const res = await VendorService.addCompliance(id!, newCompliance);
            if (res.success) {
                setShowComplianceModal(false);
                setNewCompliance({ status: 1 });
                setFormErrors(null);
                loadVendor();
                await showAlert('Record saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Compliance record was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleSavePerformance = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const res = await VendorService.addPerformance(id!, newPerformance);
            if (res.success) {
                setShowPerformanceModal(false);
                setNewPerformance({ 
                    qualityScore: 100, 
                    deliveryScore: 100, 
                    responsivenessScore: 100, 
                    complianceScore: 100 
                });
                setFormErrors(null);
                loadVendor();
                await showAlert('Review saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Review was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleSaveRisk = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setFormErrors(null);
        try {
            const res = await VendorService.addRisk(id!, newRisk);
            if (res.success) {
                setShowRiskModal(false);
                setNewRisk({ riskLevel: 1 });
                setFormErrors(null);
                loadVendor();
                await showAlert('Assessment saved.', 'Success');
            } else {
                setFormErrors([res.message || 'Assessment was not saved.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const getDocumentStatusBadge = (status: any, statusName?: string) => {
        if (status === 'Verified' || status === 3) return <span className="badge bg-success">{statusName || 'Verified'}</span>;
        if (status === 'Rejected' || status === 4) return <span className="badge bg-danger">{statusName || 'Rejected'}</span>;
        if (status === 'PendingVerification' || status === 2) return <span className="badge bg-warning text-dark">{statusName || 'Pending Verification'}</span>;
        if (status === 'Expired' || status === 5) return <span className="badge bg-secondary">{statusName || 'Expired'}</span>;
        return <span className="badge bg-secondary">{statusName || 'Draft'}</span>;
    };

    const isDocPendingVerification = (d: VendorDocument) =>
        d.status === 2 || d.status === 'PendingVerification' || d.status === 1 || d.status === 'Draft';

    const handleDownloadDocument = async (d: VendorDocument) => {
        if (!d.id) return;
        setDownloadingDocId(d.id);
        try {
            await VendorService.downloadDocument(d.id, d.originalFileName || d.title || 'document');
        } catch (err: any) {
            const msg = err?.response?.status === 403
                ? 'You do not have permission to download this document.'
                : (err?.response?.data?.message || err?.message || 'Download did not complete.');
            await showAlert(typeof msg === 'string' ? msg : 'Download did not complete.', 'Error');
        } finally {
            setDownloadingDocId(null);
        }
    };

    const handleVerifyDocument = async (d: VendorDocument, approve: boolean) => {
        if (!d.id) return;
        const comments = await showPrompt(
            approve ? 'Optional verification comments:' : 'Rejection reason (optional):',
            approve ? 'Approve Document' : 'Reject Document',
            ''
        );
        if (comments === null) return; // cancelled
        setVerifyingDocId(d.id);
        try {
            const status = approve ? 3 : 4; // Verified | Rejected
            const res = await VendorService.updateDocumentStatus(d.id, status, comments || undefined);
            if (res.success || res.Success || res.data) {
                await loadVendor();
            } else {
                await showAlert(res.message || 'Document status was not updated.', 'Error');
            }
        } catch (err: any) {
            const msg = err?.response?.status === 403
                ? 'You do not have permission to verify documents.'
                : (err?.response?.data?.message || err?.message || 'Status was not updated.');
            await showAlert(typeof msg === 'string' ? msg : 'Status was not updated.', 'Error');
        } finally {
            setVerifyingDocId(null);
        }
    };

    const deleteItem = async (type: string, itemId: string) => {
        const confirmed = await showConfirm(`Are you sure you want to delete this ${type}?`, "Confirm Deletion");
        if (!confirmed) return;
        
        try {
            let res;
            switch(type) {
                case 'contact': res = await VendorService.deleteContact(itemId); break;
                case 'address': res = await VendorService.deleteAddress(itemId); break;
                case 'bank': res = await VendorService.deleteBankAccount(itemId); break;
                case 'contract': res = await VendorService.deleteContract(itemId); break;
                case 'compliance': res = await VendorService.deleteCompliance(itemId); break;
                case 'performance': res = await VendorService.deletePerformance(itemId); break;
                case 'risk': res = await VendorService.deleteRisk(itemId); break;
                case 'document': res = await VendorService.deleteDocument(itemId); break;
                default: throw new Error("Unknown type");
            }
            if (res.success || res.Success || res.status === 200 || res.status === 204 || !res.message) {
                // Remove from state without reloading if you want to avoid network call, 
                // but reloading is safer for keeping things synced.
                loadVendor();
            } else {
                await showAlert(res.message || res.Message || 'Record was not deleted.', 'Error');
            }
        } catch (err: unknown) {
            await showAlert(getApiErrorMessages(err).join(' '), 'Error');
        }
    };

    const submitForApproval = async () => {
        const confirmed = await showConfirm('Are you sure you want to submit this vendor for approval?', "Submit for Approval");
        if (!confirmed) return;
        
        try {
            const res = await VendorService.submitForApproval(id!);
            if (res.success || res.Success || res.status === 200) {
                await showAlert('Submitted for approval.', 'Success');
                loadVendor();
            } else {
                await showAlert(res.message || res.Message || 'Submit for approval did not complete.', 'Error');
            }
        } catch (err: unknown) {
            await showAlert(getApiErrorMessages(err).join(' '), 'Error');
            // If it failed because it's already under review, refresh the data to show the banner
            loadVendor();
        }
    };

    const processWorkflowAction = async (action: string) => {
        if (!activeRequest || !canActOnCurrentStep) return;
        const comments = await showPrompt(`Enter comments for ${action.toLowerCase()}:`, `${action} Request`);
        if (comments === null) return; // user cancelled

        try {
            const res = await VendorService.processWorkflowAction(activeRequest.id, action, comments);
            if (res.success || res.Success || res.status === 200) {
                await showAlert(action === 'Reject' ? 'Rejected.' : 'Approved.', 'Success');
                loadVendor();
            } else {
                await showAlert(res.message || res.Message || `${action} did not complete.`, 'Error');
            }
        } catch (err: unknown) {
            await showAlert(getApiErrorMessages(err).join(' '), 'Error');
        }
    };

    if (loading && !vendor) return <div className="text-center py-5"><div className="spinner-border text-primary"></div></div>;
    if (error) return <div className="alert alert-danger">{error}</div>;
    if (!vendor) return null;

    return (
        <div className="container-fluid mb-5">
            <div className="card shadow-sm border-0 mb-4 bg-white">
                <div className="card-body p-4 d-flex justify-content-between align-items-center flex-wrap gap-3">
                    <div>
                        <div className="text-muted small">Vendor Number: <span className="font-mono">{vendor.vendorNumber || 'Pending Generation'}</span></div>
                        <h2 className="fw-bold mb-1 text-primary">{vendor.legalName}</h2>
                        <div className="text-muted">Trading as: {vendor.tradeName || 'N/A'}</div>
                    </div>
                    <div className="d-flex align-items-center gap-3 flex-wrap">
                        <h4 className="mb-0"><StatusPill status={vendor.statusName || vendor.status} /></h4>
                        {(typeof vendor.status === 'string' ? vendor.status.trim() === 'Draft' : vendor.status === 1) && isProcurement && (
                            <button className="btn btn-success" onClick={submitForApproval}>
                                <i className="bi bi-send-check me-2"></i> Submit for Approval
                            </button>
                        )}
                        <Link to="/vendors/directory" className="btn btn-outline-secondary">Back to List</Link>
                        {isProcurement && (
                            <Link to={`/vendors/${id}/edit`} className="btn btn-primary">Edit Vendor</Link>
                        )}
                    </div>
                </div>
            </div>

            <div className="mb-4">
                <WorkflowStepper
                    vendorStatus={vendor.status}
                    vendorStatusName={vendor.statusName}
                    workflow={activeRequest}
                />
                {workflowDecision.awaitingDecision && (
                    <div className="d-flex justify-content-between align-items-center mt-3 px-0 flex-wrap gap-2">
                        <div className="text-muted small">
                            Request: <span className="font-mono">{activeRequest?.requestNumber}</span>
                        </div>
                        <div className="text-end">
                            {canActOnCurrentStep ? (
                                <>
                                    <button
                                        className="btn btn-sm btn-success me-2"
                                        onClick={() => processWorkflowAction('Approve')}
                                    >
                                        Approve
                                    </button>
                                    <button
                                        className="btn btn-sm btn-danger"
                                        onClick={() => processWorkflowAction('Reject')}
                                    >
                                        Reject
                                    </button>
                                </>
                            ) : (
                                <div className="text-muted small">{workflowDecision.waitingOnCaption}</div>
                            )}
                        </div>
                    </div>
                )}
            </div>

            <div className="card shadow-sm border-0">
                <div className="card-header bg-white pt-3 pb-0 border-bottom-0">
                    <ul className="nav nav-tabs border-bottom">
                        {visibleTabs.map(tab => {
                            const hasData = tab !== 'overview' && vendor[tab as keyof VendorDetails] && Array.isArray(vendor[tab as keyof VendorDetails]) && (vendor[tab as keyof VendorDetails] as any[]).length > 0;
                            
                            return (
                                <li className="nav-item" key={tab}>
                                    <button 
                                        className={`nav-link text-capitalize ${activeTab === tab ? 'active fw-bold text-primary border-primary border-bottom-0 border-3' : 'text-muted'}`}
                                        onClick={() => setActiveTab(tab)}
                                        style={activeTab === tab ? { borderTopLeftRadius: '0.375rem', borderTopRightRadius: '0.375rem' } : {}}
                                    >
                                        {tab}
                                        {hasData ? ` (${(vendor[tab as keyof VendorDetails] as any[]).length})` : ''}
                                    </button>
                                </li>
                            );
                        })}
                    </ul>
                </div>
                <div className="card-body p-4">
                    
                    {/* OVERVIEW TAB */}
                    {activeTab === 'overview' && (
                        <div className="row">
                            <div className="col-md-6 mb-4">
                                <h5 className="border-bottom pb-2">Company Information</h5>
                                <table className="table table-borderless">
                                    <tbody>
                                        <tr>
                                            <td className="text-muted" style={{width: '180px'}}>Legal Name:</td>
                                            <td className="fw-bold">{vendor.legalName}</td>
                                        </tr>
                                        <tr>
                                            <td className="text-muted">Trading Name:</td>
                                            <td>{vendor.tradeName || '-'}</td>
                                        </tr>
                                        <tr>
                                            <td className="text-muted">Tax/VAT Reg. No:</td>
                                            <td>{isViewer ? <span className="text-muted fst-italic">Restricted</span> : (vendor.taxRegistrationNumber || 'Not Registered')}</td>
                                        </tr>
                                        <tr>
                                            <td className="text-muted">Website:</td>
                                            <td>{vendor.website ? <a href={vendor.website} target="_blank" rel="noreferrer" className="text-primary text-decoration-none">{vendor.website}</a> : '-'}</td>
                                        </tr>
                                        <tr>
                                            <td className="text-muted">Added On:</td>
                                            <td className="font-mono">{vendor.createdAt ? new Date(vendor.createdAt).toLocaleDateString() : 'Just now'}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div className="col-md-6 mb-4">
                                <h5 className="border-bottom pb-2">Financial Settings</h5>
                                <table className="table table-borderless">
                                    <tbody>
                                        <tr>
                                            <td className="text-muted" style={{width: '180px'}}>Preferred Currency:</td>
                                            <td className="fw-bold">{vendor.currencyCode || 'USD'}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    )}

                    {/* CONTACTS TAB */}
                    {activeTab === 'contacts' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Vendor Contacts</h5>
                                {isProcurement && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={openCreateContact}>
                                        <i className="bi bi-plus"></i> Add Contact
                                    </button>
                                )}
                            </div>
                            {vendor.contacts.length === 0 ? <div className="text-muted">No contacts configured.</div> : (
                                <table className="table table-striped">
                                    <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Status</th>{isProcurement && <th className="text-end">Actions</th>}</tr></thead>
                                    <tbody>
                                        {vendor.contacts.map(c => (
                                            <tr key={c.id}>
                                                <td className="fw-bold">{c.name} {c.isPrimary && <span className="badge bg-primary ms-1">Primary</span>}</td>
                                                <td><a href={`mailto:${c.email}`}>{c.email}</a></td>
                                                <td>{c.phone || '-'}</td>
                                                <td><span className={`badge ${c.isActive ? 'bg-success' : 'bg-danger'}`}>{c.isActive ? 'Active' : 'Inactive'}</span></td>
                                                {isProcurement && (
                                                    <td className="text-end">
                                                        <button type="button" className="btn btn-sm btn-outline-primary border-0 me-1" title="Edit" onClick={() => openEditContact(c)}>
                                                            <i className="bi bi-pencil"></i>
                                                        </button>
                                                        <button type="button" className="btn btn-sm btn-outline-danger border-0" title="Delete" onClick={() => deleteItem('contact', c.id!)}>
                                                            <i className="bi bi-trash"></i>
                                                        </button>
                                                    </td>
                                                )}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            )}
                        </div>
                    )}

                    {/* ADDRESSES TAB */}
                    {activeTab === 'addresses' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Company Locations</h5>
                                {isProcurement && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={() => { setFormErrors(null); setShowAddressModal(true); }}>
                                        <i className="bi bi-plus"></i> Add Address
                                    </button>
                                )}
                            </div>
                            <div className="row">
                                {vendor.addresses.length === 0 ? <div className="text-muted">No addresses configured.</div> : vendor.addresses.map(a => (
                                    <div className="col-md-4 mb-3" key={a.id}>
                                        <div className="card h-100 border shadow-sm position-relative">
                                            {isProcurement && (
                                                <button 
                                                    className="btn btn-sm btn-link text-danger position-absolute top-0 end-0 mt-2 me-2" 
                                                    onClick={() => deleteItem('address', a.id!)}
                                                >
                                                    <i className="bi bi-trash"></i>
                                                </button>
                                            )}
                                            <div className={`card-header bg-light fw-bold text-primary ${isProcurement ? 'pe-5' : ''}`}>{a.addressTypeName || 'Address'}</div>
                                            <div className="card-body">
                                                <p className="mb-1">{a.addressLine1}</p>
                                                {a.addressLine2 && <p className="mb-1">{a.addressLine2}</p>}
                                                <p className="mb-1">{a.city}, {a.stateProvince}</p>
                                                <p className="mb-0"><strong>{a.country}</strong> - {a.postalCode}</p>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* BANKING TAB */}
                    {activeTab === 'banking' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Banking Credentials</h5>
                                {isProcurement && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={openCreateBank}>
                                        <i className="bi bi-plus"></i> Add Account
                                    </button>
                                )}
                            </div>
                            {vendor.bankAccounts.length === 0 ? <div className="text-muted">No banking details configured.</div> : (
                                <table className="table table-bordered">
                                    <thead className="bg-light"><tr><th>Bank Name</th><th>Account Number</th><th>Currency</th><th>Status</th>{isProcurement && <th className="text-end">Actions</th>}</tr></thead>
                                    <tbody>
                                        {vendor.bankAccounts.map(b => (
                                            <tr key={b.id}>
                                                <td className="fw-bold">{b.bankName}</td>
                                                <td><code>{b.accountNumber}</code></td>
                                                <td>{b.currencyCode}</td>
                                                <td>{b.isApproved ? <span className="badge bg-success">Verified</span> : <span className="badge bg-warning text-dark">Pending</span>}</td>
                                                {isProcurement && (
                                                    <td className="text-end">
                                                        <button type="button" className="btn btn-sm btn-outline-primary border-0 me-1" title="Edit" onClick={() => openEditBank(b)}>
                                                            <i className="bi bi-pencil"></i>
                                                        </button>
                                                        <button type="button" className="btn btn-sm btn-outline-danger border-0" title="Delete" onClick={() => deleteItem('bank', b.id!)}>
                                                            <i className="bi bi-trash"></i>
                                                        </button>
                                                    </td>
                                                )}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            )}
                        </div>
                    )}
                    
                    {/* DOCUMENTS TAB */}
                    {activeTab === 'documents' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Verification Documents</h5>
                                {isProcurement && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={() => { setFormErrors(null); setShowDocumentModal(true); }}>
                                        <i className="bi bi-upload"></i> Upload
                                    </button>
                                )}
                            </div>
                            {vendor.documents.length === 0 ? <div className="text-muted">No files uploaded.</div> : (
                                <table className="table table-hover">
                                    <thead className="table-light">
                                        <tr>
                                            <th>Title</th>
                                            <th>File</th>
                                            <th>Status</th>
                                            <th>Download</th>
                                            {(canVerifyDocument || isProcurement) && <th className="text-end">Actions</th>}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {vendor.documents.map(d => (
                                            <tr key={d.id}>
                                                <td className="fw-bold">{d.title}</td>
                                                <td><i className="bi bi-file-earmark-text text-primary me-2"></i>{d.originalFileName}</td>
                                                <td>{getDocumentStatusBadge(d.status, d.statusName)}</td>
                                                <td>
                                                    {canDownloadDocument ? (
                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-primary"
                                                            disabled={downloadingDocId === d.id}
                                                            onClick={() => handleDownloadDocument(d)}
                                                            title="Download"
                                                        >
                                                            {downloadingDocId === d.id
                                                                ? <span className="spinner-border spinner-border-sm" role="status" />
                                                                : <i className="bi bi-download"></i>}
                                                        </button>
                                                    ) : (
                                                        <span className="text-muted small">—</span>
                                                    )}
                                                </td>
                                                {(canVerifyDocument || isProcurement) && (
                                                    <td className="text-end text-nowrap">
                                                        {canVerifyDocument && isDocPendingVerification(d) && (
                                                            <>
                                                                <button
                                                                    type="button"
                                                                    className="btn btn-sm btn-outline-success me-1"
                                                                    disabled={verifyingDocId === d.id}
                                                                    onClick={() => handleVerifyDocument(d, true)}
                                                                    title="Approve / Verify"
                                                                >
                                                                    <i className="bi bi-check-lg"></i>
                                                                </button>
                                                                <button
                                                                    type="button"
                                                                    className="btn btn-sm btn-outline-danger me-1"
                                                                    disabled={verifyingDocId === d.id}
                                                                    onClick={() => handleVerifyDocument(d, false)}
                                                                    title="Reject"
                                                                >
                                                                    <i className="bi bi-x-lg"></i>
                                                                </button>
                                                            </>
                                                        )}
                                                        {isProcurement && (
                                                            <button className="btn btn-sm btn-outline-danger border-0" onClick={() => deleteItem('document', d.id!)}>
                                                                <i className="bi bi-trash"></i>
                                                            </button>
                                                        )}
                                                    </td>
                                                )}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            )}
                        </div>
                    )}

                    {/* CONTRACTS TAB */}
                    {activeTab === 'contracts' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Active Agreements</h5>
                                {isProcurement && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={() => { setFormErrors(null); setShowContractModal(true); }}>
                                        <i className="bi bi-plus"></i> Add Contract
                                    </button>
                                )}
                            </div>
                            {vendor.contracts.length === 0 ? <div className="text-muted">No contracts found.</div> : vendor.contracts.map(c => (
                                <div className="card border mb-3 shadow-sm position-relative" key={c.id}>
                                    {isProcurement && (
                                        <button 
                                            className="btn btn-sm btn-link text-danger position-absolute top-0 end-0 mt-2 me-2" 
                                            onClick={() => deleteItem('contract', c.id!)}
                                        >
                                            <i className="bi bi-trash"></i>
                                        </button>
                                    )}
                                    <div className="card-body row align-items-center">
                                        <div className="col-md-4">
                                            <span className="text-muted small">{c.contractNumber}</span>
                                            <h5 className="mb-1 text-primary">{c.title}</h5>
                                            <span className="badge bg-success">{c.statusName || 'Active'}</span>
                                        </div>
                                        <div className="col-md-4 text-center">
                                            <span className="text-muted small">Term</span>
                                            <p className="mb-0 fw-bold">{new Date(c.startDate).toLocaleDateString()} - {new Date(c.endDate).toLocaleDateString()}</p>
                                        </div>
                                        <div className="col-md-4 text-end">
                                            <span className="text-muted small">Total Value</span>
                                            <p className="mb-0 fw-bold fs-5 text-success">{c.contractValue.toLocaleString()} {c.currencyCode}</p>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    {/* COMPLIANCE TAB */}
                    {activeTab === 'compliance' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Governance Requirements</h5>
                                {isRiskAndCompliance && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={() => { setFormErrors(null); setShowComplianceModal(true); }}>
                                        <i className="bi bi-plus"></i> Add Compliance Log
                                    </button>
                                )}
                            </div>
                            {vendor.compliances.length === 0 ? <div className="text-muted">No checks logged.</div> : vendor.compliances.map(c => (
                                <div className="card border mb-3 position-relative" key={c.id}>
                                    {isRiskAndCompliance && (
                                        <button 
                                            className="btn btn-sm btn-link text-danger position-absolute top-0 end-0 mt-2 me-2" 
                                            onClick={() => deleteItem('compliance', c.id!)}
                                        >
                                            <i className="bi bi-trash"></i>
                                        </button>
                                    )}
                                    <div className="card-body d-flex justify-content-between pe-5">
                                        <div><h6 className="fw-bold mb-1">{c.requirementName}</h6><p className="mb-0 text-muted small">{c.notes}</p></div>
                                        <span className={`badge ${c.status === 2 ? 'bg-danger' : 'bg-success'}`}>{c.statusName}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    {/* PERFORMANCE TAB */}
                    {activeTab === 'performance' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Quarterly Scorecards</h5>
                                {isProcurement && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={() => { setFormErrors(null); setShowPerformanceModal(true); }}>
                                        <i className="bi bi-plus"></i> Add Performance Review
                                    </button>
                                )}
                            </div>
                            {vendor.performances.length === 0 ? <div className="text-muted">No scores recorded.</div> : vendor.performances.map(p => (
                                <div className="card border mb-4 position-relative" key={p.id}>
                                    {isProcurement && (
                                        <button 
                                            className="btn btn-sm btn-link text-danger position-absolute top-0 end-0 mt-1 me-1" 
                                            onClick={() => deleteItem('performance', p.id!)}
                                            style={{ zIndex: 10 }}
                                        >
                                            <i className="bi bi-trash"></i>
                                        </button>
                                    )}
                                    <div className="card-header bg-light d-flex justify-content-between pe-5">
                                        <span className="fw-bold">{p.evaluationPeriod}</span>
                                        <span className="badge bg-primary px-3">Avg: {p.averageScore?.toFixed(1)} / 100</span>
                                    </div>
                                    <div className="card-body text-center row">
                                        <div className="col-3 border-end"><span className="text-muted d-block small">Quality</span><strong>{p.qualityScore}</strong></div>
                                        <div className="col-3 border-end"><span className="text-muted d-block small">Delivery</span><strong>{p.deliveryScore}</strong></div>
                                        <div className="col-3 border-end"><span className="text-muted d-block small">SLA</span><strong>{p.responsivenessScore}</strong></div>
                                        <div className="col-3"><span className="text-muted d-block small">Compliance</span><strong>{p.complianceScore}</strong></div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    {/* RISK TAB */}
                    {activeTab === 'risk' && (
                        <div>
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <h5>Risk Assessments</h5>
                                {isRiskAndCompliance && (
                                    <button className="btn btn-sm btn-outline-primary" onClick={() => { setFormErrors(null); setShowRiskModal(true); }}>
                                        <i className="bi bi-plus"></i> Add Risk Log
                                    </button>
                                )}
                            </div>
                            {vendor.risks.length === 0 ? <div className="text-muted">No assessment history.</div> : vendor.risks.map(r => (
                                <div className="card border mb-3 position-relative" key={r.id}>
                                    {isRiskAndCompliance && (
                                        <button 
                                            className="btn btn-sm btn-link text-danger position-absolute top-0 end-0 mt-2 me-2" 
                                            onClick={() => deleteItem('risk', r.id!)}
                                        >
                                            <i className="bi bi-trash"></i>
                                        </button>
                                    )}
                                    <div className="card-body d-flex justify-content-between pe-5">
                                        <div>
                                            <h6 className="fw-bold mb-1">{r.riskCategory}</h6>
                                            <p className="mb-2 text-muted small">{r.riskDescription}</p>
                                            <p className="mb-0 text-success small"><strong>Mitigation:</strong> {r.mitigationPlan}</p>
                                        </div>
                                        <span className={`badge ${r.riskLevelName === 'Critical' ? 'bg-danger' : 'bg-warning'}`}>{r.riskLevelName} Risk</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                </div>
            </div>

            {/* MODALS */}
            {showContactModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSaveContact}>
                                <div className="modal-header">
                                    <h5 className="modal-title">{editingContactId ? 'Edit Contact' : 'Add New Contact'}</h5>
                                    <button type="button" className="btn-close" onClick={closeContactModal}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Name *</label>
                                        <input type="text" className="form-control" required value={newContact.name || ''} onChange={e => setNewContact({...newContact, name: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Email *</label>
                                        <input type="email" className="form-control" required value={newContact.email || ''} onChange={e => setNewContact({...newContact, email: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Job Title</label>
                                        <input type="text" className="form-control" value={newContact.jobTitle || ''} onChange={e => setNewContact({...newContact, jobTitle: e.target.value})} />
                                    </div>
                                    <div className="row">
                                        <div className="col-md-6 mb-3">
                                            <label className="form-label">Phone</label>
                                            <input type="text" className="form-control" value={newContact.phone || ''} onChange={e => setNewContact({...newContact, phone: e.target.value})} />
                                        </div>
                                        <div className="col-md-6 mb-3">
                                            <label className="form-label">Mobile</label>
                                            <input type="text" className="form-control" value={newContact.mobile || ''} onChange={e => setNewContact({...newContact, mobile: e.target.value})} />
                                        </div>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Contact Type</label>
                                        <select
                                            className="form-select"
                                            value={Number(newContact.contactType ?? 1)}
                                            onChange={e => setNewContact({...newContact, contactType: parseInt(e.target.value, 10)})}
                                        >
                                            <option value={1}>Sales</option>
                                            <option value={2}>Finance</option>
                                            <option value={3}>Technical</option>
                                            <option value={4}>Legal</option>
                                            <option value={5}>Management</option>
                                        </select>
                                    </div>
                                    <div className="mb-3 form-check">
                                        <input type="checkbox" className="form-check-input" id="isPrimaryCheck" checked={!!newContact.isPrimary} onChange={e => setNewContact({...newContact, isPrimary: e.target.checked})} />
                                        <label className="form-check-label" htmlFor="isPrimaryCheck">Primary Contact</label>
                                    </div>
                                    <div className="mb-3 form-check">
                                        <input type="checkbox" className="form-check-input" id="isActiveCheck" checked={newContact.isActive !== false} onChange={e => setNewContact({...newContact, isActive: e.target.checked})} />
                                        <label className="form-check-label" htmlFor="isActiveCheck">Active</label>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={closeContactModal}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>
                                        {saving ? 'Saving...' : (editingContactId ? 'Update Contact' : 'Save Contact')}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showAddressModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSaveAddress}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Add New Address</h5>
                                    <button type="button" className="btn-close" onClick={() => { setFormErrors(null); setShowAddressModal(false); }}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Address Type</label>
                                        <select className="form-select" value={newAddress.addressType} onChange={e => setNewAddress({...newAddress, addressType: parseInt(e.target.value)})}>
                                            <option value={1}>Headquarters</option>
                                            <option value={2}>Billing</option>
                                            <option value={3}>Shipping</option>
                                            <option value={4}>Branch Office</option>
                                        </select>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Address Line 1 *</label>
                                        <input type="text" className="form-control" required value={newAddress.addressLine1 || ''} onChange={e => setNewAddress({...newAddress, addressLine1: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">City *</label>
                                        <input type="text" className="form-control" required value={newAddress.city || ''} onChange={e => setNewAddress({...newAddress, city: e.target.value})} />
                                    </div>
                                    <div className="row">
                                        <div className="col-6 mb-3">
                                            <label className="form-label">State / Province</label>
                                            <input type="text" className="form-control" value={newAddress.stateProvince || ''} onChange={e => setNewAddress({...newAddress, stateProvince: e.target.value})} />
                                        </div>
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Postal Code</label>
                                            <input type="text" className="form-control" value={newAddress.postalCode || ''} onChange={e => setNewAddress({...newAddress, postalCode: e.target.value})} />
                                        </div>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Country *</label>
                                        <input type="text" className="form-control" required value={newAddress.country || ''} onChange={e => setNewAddress({...newAddress, country: e.target.value})} />
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={() => { setFormErrors(null); setShowAddressModal(false); }}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save Address'}</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showBankModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSaveBank}>
                                <div className="modal-header">
                                    <h5 className="modal-title">{editingBankId ? 'Edit Bank Account' : 'Add Bank Account'}</h5>
                                    <button type="button" className="btn-close" onClick={closeBankModal}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Bank Name *</label>
                                        <input type="text" className="form-control" required value={newBank.bankName || ''} onChange={e => setNewBank({...newBank, bankName: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Account Name</label>
                                        <input type="text" className="form-control" value={newBank.accountName || ''} onChange={e => setNewBank({...newBank, accountName: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Account Number *</label>
                                        <input type="text" className="form-control" required value={newBank.accountNumber || ''} onChange={e => setNewBank({...newBank, accountNumber: e.target.value})} />
                                    </div>
                                    <div className="row">
                                        <div className="col-6 mb-3">
                                            <label className="form-label">IBAN</label>
                                            <input type="text" className="form-control" value={newBank.iban || ''} onChange={e => setNewBank({...newBank, iban: e.target.value})} />
                                        </div>
                                        <div className="col-6 mb-3">
                                            <label className="form-label">SWIFT / BIC</label>
                                            <input type="text" className="form-control" value={newBank.swiftBic || ''} onChange={e => setNewBank({...newBank, swiftBic: e.target.value})} />
                                        </div>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Currency</label>
                                        <input type="text" className="form-control" maxLength={3} value={newBank.currencyCode || ''} onChange={e => setNewBank({...newBank, currencyCode: e.target.value})} />
                                    </div>
                                    <div className="mb-3 form-check">
                                        <input type="checkbox" className="form-check-input" id="isVerifiedCheck" checked={!!newBank.isVerified} onChange={e => setNewBank({...newBank, isVerified: e.target.checked})} />
                                        <label className="form-check-label" htmlFor="isVerifiedCheck">Verified</label>
                                    </div>
                                    <div className="mb-3 form-check">
                                        <input type="checkbox" className="form-check-input" id="isApprovedCheck" checked={!!newBank.isApproved} onChange={e => setNewBank({...newBank, isApproved: e.target.checked})} />
                                        <label className="form-check-label" htmlFor="isApprovedCheck">Approved</label>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={closeBankModal}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>
                                        {saving ? 'Saving...' : (editingBankId ? 'Update Account' : 'Save Account')}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showDocumentModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleUploadDocument}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Upload Document</h5>
                                    <button type="button" className="btn-close" onClick={() => { setFormErrors(null); setShowDocumentModal(false); }}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Document Title *</label>
                                        <input type="text" className="form-control" required value={newDocument.title} onChange={e => setNewDocument({...newDocument, title: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Document Type</label>
                                        <select className="form-select" value={newDocument.type} onChange={e => setNewDocument({...newDocument, type: parseInt(e.target.value)})}>
                                            <option value={1}>W9 / Tax Form</option>
                                            <option value={2}>Insurance Certificate</option>
                                            <option value={3}>Business License</option>
                                            <option value={4}>NDA / Agreement</option>
                                            <option value={5}>Other</option>
                                        </select>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Expiration Date (Optional)</label>
                                        <input type="date" className="form-control" value={newDocument.expiryDate} onChange={e => setNewDocument({...newDocument, expiryDate: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Select File *</label>
                                        <input type="file" className="form-control" required onChange={e => setNewDocument({...newDocument, file: e.target.files ? e.target.files[0] : null})} />
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={() => { setFormErrors(null); setShowDocumentModal(false); }}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={uploading}>{uploading ? 'Uploading...' : 'Upload'}</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showContractModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSaveContract}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Add Contract</h5>
                                    <button type="button" className="btn-close" onClick={() => { setFormErrors(null); setShowContractModal(false); }}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Contract Number *</label>
                                        <input type="text" className="form-control" required value={newContract.contractNumber || ''} onChange={e => setNewContract({...newContract, contractNumber: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Title *</label>
                                        <input type="text" className="form-control" required value={newContract.title || ''} onChange={e => setNewContract({...newContract, title: e.target.value})} />
                                    </div>
                                    <div className="row">
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Start Date *</label>
                                            <input type="date" className="form-control" required value={newContract.startDate || ''} onChange={e => setNewContract({...newContract, startDate: e.target.value})} />
                                        </div>
                                        <div className="col-6 mb-3">
                                            <label className="form-label">End Date *</label>
                                            <input type="date" className="form-control" required value={newContract.endDate || ''} onChange={e => setNewContract({...newContract, endDate: e.target.value})} />
                                        </div>
                                    </div>
                                    <div className="row">
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Value *</label>
                                            <input type="number" className="form-control" required value={newContract.contractValue || ''} onChange={e => setNewContract({...newContract, contractValue: parseFloat(e.target.value)})} />
                                        </div>
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Currency</label>
                                            <input type="text" className="form-control" maxLength={3} value={newContract.currencyCode || ''} onChange={e => setNewContract({...newContract, currencyCode: e.target.value})} />
                                        </div>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={() => { setFormErrors(null); setShowContractModal(false); }}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save Contract'}</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showComplianceModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSaveCompliance}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Add Compliance Record</h5>
                                    <button type="button" className="btn-close" onClick={() => { setFormErrors(null); setShowComplianceModal(false); }}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Requirement Name *</label>
                                        <input type="text" className="form-control" required value={newCompliance.requirementName || ''} onChange={e => setNewCompliance({...newCompliance, requirementName: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Status</label>
                                        <select className="form-select" value={newCompliance.status} onChange={e => setNewCompliance({...newCompliance, status: parseInt(e.target.value)})}>
                                            <option value={1}>Compliant</option>
                                            <option value={2}>NonCompliant</option>
                                            <option value={3}>PendingReview</option>
                                            <option value={4}>Exempt</option>
                                        </select>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Notes</label>
                                        <textarea className="form-control" rows={3} value={newCompliance.notes || ''} onChange={e => setNewCompliance({...newCompliance, notes: e.target.value})}></textarea>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={() => { setFormErrors(null); setShowComplianceModal(false); }}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save Record'}</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showPerformanceModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSavePerformance}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Add Performance Review</h5>
                                    <button type="button" className="btn-close" onClick={() => { setFormErrors(null); setShowPerformanceModal(false); }}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Evaluation Period (e.g. Q1 2026) *</label>
                                        <input type="text" className="form-control" required value={newPerformance.evaluationPeriod || ''} onChange={e => setNewPerformance({...newPerformance, evaluationPeriod: e.target.value})} />
                                    </div>
                                    <div className="row">
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Quality Score (0-100) *</label>
                                            <input type="number" className="form-control" required min="0" max="100" value={newPerformance.qualityScore} onChange={e => setNewPerformance({...newPerformance, qualityScore: parseFloat(e.target.value)})} />
                                        </div>
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Delivery Score (0-100) *</label>
                                            <input type="number" className="form-control" required min="0" max="100" value={newPerformance.deliveryScore} onChange={e => setNewPerformance({...newPerformance, deliveryScore: parseFloat(e.target.value)})} />
                                        </div>
                                    </div>
                                    <div className="row">
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Responsiveness Score (0-100) *</label>
                                            <input type="number" className="form-control" required min="0" max="100" value={newPerformance.responsivenessScore} onChange={e => setNewPerformance({...newPerformance, responsivenessScore: parseFloat(e.target.value)})} />
                                        </div>
                                        <div className="col-6 mb-3">
                                            <label className="form-label">Compliance Score (0-100) *</label>
                                            <input type="number" className="form-control" required min="0" max="100" value={newPerformance.complianceScore} onChange={e => setNewPerformance({...newPerformance, complianceScore: parseFloat(e.target.value)})} />
                                        </div>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={() => { setFormErrors(null); setShowPerformanceModal(false); }}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save Review'}</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}

            {showRiskModal && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleSaveRisk}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Add Risk Assessment</h5>
                                    <button type="button" className="btn-close" onClick={() => { setFormErrors(null); setShowRiskModal(false); }}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label">Risk Category *</label>
                                        <input type="text" className="form-control" required value={newRisk.riskCategory || ''} onChange={e => setNewRisk({...newRisk, riskCategory: e.target.value})} />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Risk Level</label>
                                        <select className="form-select" value={newRisk.riskLevel} onChange={e => setNewRisk({...newRisk, riskLevel: parseInt(e.target.value)})}>
                                            <option value={1}>Low</option>
                                            <option value={2}>Medium</option>
                                            <option value={3}>High</option>
                                            <option value={4}>Critical</option>
                                        </select>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Description *</label>
                                        <textarea className="form-control" rows={2} required value={newRisk.riskDescription || ''} onChange={e => setNewRisk({...newRisk, riskDescription: e.target.value})}></textarea>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Mitigation Plan</label>
                                        <textarea className="form-control" rows={2} value={newRisk.mitigationPlan || ''} onChange={e => setNewRisk({...newRisk, mitigationPlan: e.target.value})}></textarea>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={() => { setFormErrors(null); setShowRiskModal(false); }}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save Assessment'}</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};