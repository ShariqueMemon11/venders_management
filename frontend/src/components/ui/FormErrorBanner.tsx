import React from 'react';

type FormErrorBannerProps = {
    messages: string[] | null | undefined;
    title?: string;
};

/** Temporary stopgap — shows ProblemDetails messages until RHF+Zod field mapping lands. */
export const FormErrorBanner: React.FC<FormErrorBannerProps> = ({
    messages,
    title,
}) => {
    if (!messages || messages.length === 0) return null;

    return (
        <div className="alert alert-danger" role="alert">
            {title ? <div className="fw-semibold mb-1">{title}</div> : null}
            {messages.length === 1 ? (
                <div>{messages[0]}</div>
            ) : (
                <ul className="mb-0 ps-3">
                    {messages.map((m, i) => (
                        <li key={`${i}-${m.slice(0, 24)}`}>{m}</li>
                    ))}
                </ul>
            )}
        </div>
    );
};
