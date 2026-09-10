import React, { createContext, useContext, useState } from 'react';
import type { ReactNode } from 'react';

type ModalType = 'alert' | 'confirm' | 'prompt';

interface ModalState {
    isOpen: boolean;
    type: ModalType;
    title: string;
    message: string;
    defaultValue?: string;
    resolve?: (value: any) => void;
}

interface ModalContextType {
    showAlert: (message: string, title?: string) => Promise<void>;
    showConfirm: (message: string, title?: string) => Promise<boolean>;
    showPrompt: (message: string, title?: string, defaultValue?: string) => Promise<string | null>;
}

const ModalContext = createContext<ModalContextType | undefined>(undefined);

export const useModal = () => {
    const context = useContext(ModalContext);
    if (!context) {
        throw new Error('useModal must be used within a ModalProvider');
    }
    return context;
};

export const ModalProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [modalState, setModalState] = useState<ModalState>({
        isOpen: false,
        type: 'alert',
        title: '',
        message: ''
    });
    
    const [inputValue, setInputValue] = useState('');

    const showAlert = (message: string, title: string = 'Alert'): Promise<void> => {
        return new Promise((resolve) => {
            setModalState({ isOpen: true, type: 'alert', title, message, resolve });
        });
    };

    const showConfirm = (message: string, title: string = 'Confirm'): Promise<boolean> => {
        return new Promise((resolve) => {
            setModalState({ isOpen: true, type: 'confirm', title, message, resolve });
        });
    };

    const showPrompt = (message: string, title: string = 'Input Required', defaultValue: string = ''): Promise<string | null> => {
        return new Promise((resolve) => {
            setInputValue(defaultValue);
            setModalState({ isOpen: true, type: 'prompt', title, message, defaultValue, resolve });
        });
    };

    const handleClose = (result: any) => {
        setModalState((prev) => ({ ...prev, isOpen: false }));
        if (modalState.resolve) {
            modalState.resolve(result);
        }
    };

    return (
        <ModalContext.Provider value={{ showAlert, showConfirm, showPrompt }}>
            {children}
            {modalState.isOpen && (
                <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog modal-dialog-centered">
                        <div className="modal-content shadow">
                            <div className={`modal-header ${modalState.title === 'Success' ? 'bg-success text-white' : modalState.type === 'alert' ? 'bg-danger text-white' : 'bg-primary text-white'}`}>
                                <h5 className="modal-title">
                                    {modalState.title === 'Success' && <i className="bi bi-check-circle-fill me-2"></i>}
                                    {modalState.type === 'alert' && modalState.title !== 'Success' && <i className="bi bi-exclamation-triangle-fill me-2"></i>}
                                    {modalState.type === 'confirm' && <i className="bi bi-question-circle-fill me-2"></i>}
                                    {modalState.type === 'prompt' && <i className="bi bi-pencil-square me-2"></i>}
                                    {modalState.title}
                                </h5>
                                <button type="button" className="btn-close btn-close-white" onClick={() => handleClose(modalState.type === 'prompt' ? null : false)}></button>
                            </div>
                            <div className="modal-body p-4">
                                <p className="mb-3">{modalState.message}</p>
                                {modalState.type === 'prompt' && (
                                    <input 
                                        type="text" 
                                        className="form-control" 
                                        value={inputValue} 
                                        onChange={(e) => setInputValue(e.target.value)} 
                                        autoFocus
                                        onKeyDown={(e) => {
                                            if (e.key === 'Enter') handleClose(inputValue);
                                            if (e.key === 'Escape') handleClose(null);
                                        }}
                                    />
                                )}
                            </div>
                            <div className="modal-footer bg-light">
                                {modalState.type === 'alert' && (
                                    <button type="button" className={`btn ${modalState.title === 'Success' ? 'btn-success' : 'btn-secondary'}`} onClick={() => handleClose(true)}>OK</button>
                                )}
                                {modalState.type === 'confirm' && (
                                    <>
                                        <button type="button" className="btn btn-outline-secondary" onClick={() => handleClose(false)}>Cancel</button>
                                        <button type="button" className="btn btn-primary" onClick={() => handleClose(true)}>Confirm</button>
                                    </>
                                )}
                                {modalState.type === 'prompt' && (
                                    <>
                                        <button type="button" className="btn btn-outline-secondary" onClick={() => handleClose(null)}>Cancel</button>
                                        <button type="button" className="btn btn-primary" onClick={() => handleClose(inputValue)}>Submit</button>
                                    </>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </ModalContext.Provider>
    );
};