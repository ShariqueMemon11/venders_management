import React from 'react';
import './empty-state.css';

type EmptyStateProps = {
    message: string;
    actionLabel?: string;
    onAction?: () => void;
};

export const EmptyState: React.FC<EmptyStateProps> = ({ message, actionLabel, onAction }) => {
    return (
        <div className="empty-state">
            <p className="empty-state__message mb-0">{message}</p>
            {actionLabel && onAction && (
                <button type="button" className="btn btn-outline-primary btn-sm mt-3" onClick={onAction}>
                    {actionLabel}
                </button>
            )}
        </div>
    );
};
