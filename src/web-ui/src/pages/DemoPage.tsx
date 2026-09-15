import { useCallback, useMemo } from 'react';
import {
    getDemoDevices,
    getGoldenQueries,
    isSearchStage,
    type GoldenQuery,
    type SearchStage,
} from '@/api/client';
import { AppHeader } from '@/components/AppHeader';
import { HowItWorksTab } from '@/components/HowItWorksTab';
import { PipelineStepper } from '@/components/PipelineStepper';
import { ResultsTab } from '@/components/ResultsTab';
import { SearchBar } from '@/components/SearchBar';
import { SearchOutcome } from '@/components/SearchOutcome';
import { StageOptions } from '@/components/StageOptions';
import { StageTabs } from '@/components/StageTabs';
import { UnderTheHoodTab } from '@/components/UnderTheHoodTab';
import { useApiData } from '@/hooks/useApiData';
import { usePipelineSearch } from '@/hooks/usePipelineSearch';
import { useSearchState } from '@/hooks/useSearchState';
import {
    applyGoldenQuery,
    countActiveFilters,
    toSearchRequest,
    type SearchState,
    type StageTab,
} from '@/lib/searchState';
import { isTabAvailable } from '@/lib/stageTabs';

/**
 * `/demo`: the free-exploration stage screen (ADR-0014 § Pages). Every input lives in the URL, and the
 * same request goes to whichever stage is selected.
 */
export function DemoPage() {
    const [state, setState] = useSearchState();
    const goldenQueries = useApiData(getGoldenQueries);
    const devices = useApiData(getDemoDevices);

    // TODO(Phase 4): Stages 6–7 get endpoints in Phase 4; until then a URL naming them shows Stage 5.
    const stage: SearchStage = isSearchStage(state.stage) ? state.stage : 'ontology';
    const tab: StageTab = isTabAvailable(state.tab, stage) ? state.tab : 'results';
    const request = useMemo(() => toSearchRequest(state), [state]);
    const search = usePipelineSearch(stage, request);

    const update = useCallback(
        (change: Partial<SearchState>) => setState({ ...state, ...change }),
        [setState, state],
    );
    const handleChooseTab = useCallback((next: StageTab) => update({ tab: next }), [update]);

    function handleSubmitQuery(query: string) {
        // Editing a golden query's text makes it an ordinary query, so the picker stops naming the preset.
        const preset = goldenQueries.data?.find((goldenQuery) => goldenQuery.id === state.goldenQueryId);
        update({ query, goldenQueryId: preset?.request.query === query ? state.goldenQueryId : null });
    }

    function handleChooseGoldenQuery(goldenQuery: GoldenQuery | null) {
        setState(
            goldenQuery === null ? { ...state, goldenQueryId: null } : applyGoldenQuery(state, goldenQuery),
        );
    }

    const counts: Partial<Record<StageTab, string>> =
        search.response === null
            ? {}
            : {
                  results: String(search.response.totalResults),
                  'under-the-hood': `${search.response.debugTrace.steps.length} steps`,
              };

    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader />
            <main className="flex flex-1 flex-col gap-2.5 px-6 pt-3 pb-4">
                <SearchBar
                    query={state.query}
                    goldenQueryId={state.goldenQueryId}
                    targetProductId={state.targetProductId}
                    activeFilterCount={countActiveFilters(state.filters)}
                    goldenQueries={goldenQueries.data ?? []}
                    devices={devices.data ?? []}
                    onSubmitQuery={handleSubmitQuery}
                    onChooseGoldenQuery={handleChooseGoldenQuery}
                    onChooseDevice={(productId) => update({ targetProductId: productId })}
                />
                <PipelineStepper stage={stage} onChooseStage={(next) => update({ stage: next })} />
                <StageTabs
                    stage={stage}
                    tab={tab}
                    onChooseTab={handleChooseTab}
                    counts={counts}
                    options={
                        <StageOptions
                            stage={stage}
                            expandSynonyms={state.expandSynonyms}
                            applyConstraints={state.applyConstraints}
                            onChange={update}
                        />
                    }
                    panels={{
                        'how-it-works': <HowItWorksTab stage={stage} />,
                        results: (
                            <SearchOutcome search={search}>
                                {(response) => <ResultsTab response={response} />}
                            </SearchOutcome>
                        ),
                        // TODO(Phase 4): AnswerPanel, ExplanationPanel and EvidenceSet (the tab is disabled until Stage 6).
                        answer: null,
                        'under-the-hood': (
                            <SearchOutcome search={search}>
                                {(response) => <UnderTheHoodTab response={response} />}
                            </SearchOutcome>
                        ),
                    }}
                />
            </main>
        </div>
    );
}
