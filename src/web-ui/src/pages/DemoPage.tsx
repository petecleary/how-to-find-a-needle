import { getGoldenQueries } from '@/api/client';
import { AppHeader } from '@/components/AppHeader';
import { StageScreen } from '@/components/StageScreen';
import { useApiData } from '@/hooks/useApiData';
import { useSearchState } from '@/hooks/useSearchState';

/**
 * `/demo`: the free-exploration stage screen (ADR-0014 § Pages). Every input lives in the URL, so a moment can
 * be bookmarked and Back undoes a change.
 */
export function DemoPage() {
    const [state, setState] = useSearchState();
    const goldenQueries = useApiData(getGoldenQueries);

    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader />
            <StageScreen
                state={state}
                onStateChange={setState}
                goldenQueries={goldenQueries}
                filterLayout="responsive"
            />
        </div>
    );
}
