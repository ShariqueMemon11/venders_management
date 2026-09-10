/**
 * Parse API errors for temporary form banners.
 * Prefers ProblemDetails "errors" (FluentValidation); falls back to message/string.
 * Field-level mapping is deferred to React Hook Form + Zod (Batch 5).
 */
export function getApiErrorMessages(err: unknown): string[] {
    const anyErr = err as {
        response?: { data?: any; status?: number };
        message?: string;
    };
    const data = anyErr?.response?.data;

    if (data?.errors && typeof data.errors === 'object' && !Array.isArray(data.errors)) {
        const messages: string[] = [];
        for (const [field, value] of Object.entries(data.errors as Record<string, unknown>)) {
            if (Array.isArray(value)) {
                for (const msg of value) {
                    if (typeof msg === 'string' && msg.trim()) {
                        messages.push(msg.trim());
                    }
                }
            } else if (typeof value === 'string' && value.trim()) {
                messages.push(value.trim());
            } else if (field && value != null) {
                messages.push(String(value));
            }
        }
        if (messages.length) return messages;
    }

    if (typeof data?.title === 'string' && data.title && data.status === 400) {
        // ProblemDetails without a parseable errors bag
        if (typeof data.detail === 'string' && data.detail.trim()) return [data.detail.trim()];
    }

    if (typeof data?.message === 'string' && data.message.trim()) {
        return [data.message.trim()];
    }
    if (typeof data === 'string' && data.trim()) {
        return [data.trim()];
    }
    if (typeof anyErr?.message === 'string' && anyErr.message.trim()) {
        return [anyErr.message.trim()];
    }
    return ['The request did not complete. Try again.'];
}

export function formatApiErrorMessage(err: unknown): string {
    return getApiErrorMessages(err).join(' ');
}
