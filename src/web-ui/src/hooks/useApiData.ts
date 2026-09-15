import { useEffect, useState } from 'react';

export interface ApiData<T> {
    status: 'loading' | 'success' | 'error';
    data: T | null;
    error: Error | null;
}

/**
 * Loads one read-only resource when the screen opens (the golden queries, the target devices), and
 * cancels the request if the screen closes first.
 *
 * Pass a function defined outside the component, such as `getGoldenQueries`: a new function on every
 * render would load again on every render.
 */
export function useApiData<T>(load: (signal: AbortSignal) => Promise<T>): ApiData<T> {
    const [result, setResult] = useState<ApiData<T>>({ status: 'loading', data: null, error: null });

    useEffect(() => {
        const controller = new AbortController();

        load(controller.signal).then(
            (data) => {
                if (!controller.signal.aborted) {
                    setResult({ status: 'success', data, error: null });
                }
            },
            (error: unknown) => {
                if (!controller.signal.aborted) {
                    setResult({
                        status: 'error',
                        data: null,
                        error: error instanceof Error ? error : new Error(String(error)),
                    });
                }
            },
        );

        return () => controller.abort();
    }, [load]);

    return result;
}
