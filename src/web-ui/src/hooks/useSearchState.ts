import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import { parseSearchState, serializeSearchState, type SearchState } from '@/lib/searchState';

/**
 * The demo's state, read from and written to the URL's query string (ADR-0014 § State and data flow).
 * Every change adds a browser history entry, so Back undoes it.
 */
export function useSearchState(): [SearchState, (next: SearchState) => void] {
    const [params, setParams] = useSearchParams();

    const state = useMemo(() => parseSearchState(params), [params]);
    const setState = useCallback((next: SearchState) => setParams(serializeSearchState(next)), [setParams]);

    return [state, setState];
}
