import { useCallback, useMemo, useState, type ReactNode } from 'react';
import {
    getBrands,
    getDemoDevices,
    getTaxonomy,
    getVocabularies,
    isSearchStage,
    type GoldenQuery,
    type SearchStage,
} from '@/api/client';
import { FilterDrawer } from '@/components/FilterDrawer';
import { FilterPanel } from '@/components/FilterPanel';
import { HowItWorksTab } from '@/components/HowItWorksTab';
import { PipelineStepper } from '@/components/PipelineStepper';
import { ResultsTab } from '@/components/ResultsTab';
import { SearchBar } from '@/components/SearchBar';
import { SearchOutcome } from '@/components/SearchOutcome';
import { StageOptions } from '@/components/StageOptions';
import { StageTabs } from '@/components/StageTabs';
import { UnderTheHoodTab } from '@/components/UnderTheHoodTab';
import { useApiData, type ApiData } from '@/hooks/useApiData';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { usePipelineSearch } from '@/hooks/usePipelineSearch';
import {
    applyGoldenQuery,
    countActiveFilters,
    matchesGoldenQuery,
    toSearchRequest,
    type SearchState,
    type StageTab,
} from '@/lib/searchState';
import { isTabAvailable } from '@/lib/stageTabs';

// Tailwind's `lg` breakpoint: from here the filters fit beside the results; below it they open as a drawer.
const sidebarMediaQuery = '(min-width: 64rem)';

export interface StageScreenProps {
    state: SearchState;
    onStateChange: (next: SearchState) => void;
    goldenQueries: ApiData<GoldenQuery[]>;
    /** The demo shows a sidebar when there is room; talk mode always uses the drawer, so results get the width. */
    filterLayout: 'responsive' | 'drawer';
    /** A line above the tabs: the talk step's caption. */
    caption?: ReactNode;
}

/**
 * The stage screen shared by the demo and talk mode (ADR-0014 § Stage screen): search bar, pipeline stepper and
 * the four tabs. The page owns the state (the URL in the demo, the talk step in talk mode); the same request goes
 * to whichever stage is selected.
 */
export function StageScreen({
    state,
    onStateChange,
    goldenQueries,
    filterLayout,
    caption,
}: StageScreenProps) {
    const devices = useApiData(getDemoDevices);
    const brands = useApiData(getBrands);
    const taxonomy = useApiData(getTaxonomy);
    const vocabularies = useApiData(getVocabularies);

    // Whether the filters are showing is a layout preference, not a search input, so it isn't in the URL.
    const isWideScreen = useMediaQuery(sidebarMediaQuery);
    const hasRoomForSidebar = filterLayout === 'responsive' && isWideScreen;
    // Closed at first, so the results get the full width on a 1280×720 projector; the Filters button opens it.
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const isFilterPanelOpen = hasRoomForSidebar ? isSidebarOpen : isDrawerOpen;

    // TODO(Phase 4): Stages 6–7 get endpoints in Phase 4; until then a URL naming them shows Stage 5.
    const stage: SearchStage = isSearchStage(state.stage) ? state.stage : 'ontology';
    const tab: StageTab = isTabAvailable(state.tab, stage) ? state.tab : 'results';
    const request = useMemo(() => toSearchRequest(state), [state]);
    const search = usePipelineSearch(stage, request);

    const update = useCallback(
        (change: Partial<SearchState>) => onStateChange({ ...state, ...change }),
        [onStateChange, state],
    );
    const handleChooseTab = useCallback((next: StageTab) => update({ tab: next }), [update]);

    // Changing the query, device or filters away from a golden query's preset makes it an ordinary
    // search, so the picker stops naming the preset.
    function updateInputs(change: Partial<Pick<SearchState, 'query' | 'targetProductId' | 'filters'>>) {
        const next = { ...state, ...change };
        const preset = goldenQueries.data?.find((goldenQuery) => goldenQuery.id === next.goldenQueryId);
        const isStillPreset = preset !== undefined && matchesGoldenQuery(next, preset);

        onStateChange({ ...next, goldenQueryId: isStillPreset ? next.goldenQueryId : null });
    }

    function handleChooseGoldenQuery(goldenQuery: GoldenQuery | null) {
        onStateChange(
            goldenQuery === null ? { ...state, goldenQueryId: null } : applyGoldenQuery(state, goldenQuery),
        );
    }

    function handleToggleFilters() {
        if (hasRoomForSidebar) {
            setIsSidebarOpen(!isSidebarOpen);
        } else {
            setIsDrawerOpen(!isDrawerOpen);
        }
    }

    const stepCount = search.response?.debugTrace.steps.length ?? 0;
    const counts: Partial<Record<StageTab, string>> =
        search.response === null
            ? {}
            : {
                  results: String(search.response.totalResults),
                  'under-the-hood': `${stepCount} ${stepCount === 1 ? 'step' : 'steps'}`,
              };

    const filterPanel = (
        <FilterPanel
            filters={state.filters}
            brands={brands}
            taxonomy={taxonomy}
            vocabularies={vocabularies}
            onChange={(filters) => updateInputs({ filters })}
        />
    );

    return (
        <>
            <div className="flex flex-1">
                {hasRoomForSidebar && isSidebarOpen ? (
                    <aside
                        aria-label="Filters"
                        className="sticky top-0 max-h-screen w-72 flex-none self-start overflow-y-auto border-r-2 bg-card px-5 py-4"
                    >
                        {filterPanel}
                    </aside>
                ) : null}
                <main className="flex min-w-0 flex-1 flex-col gap-2.5 px-6 pt-3 pb-4">
                    <SearchBar
                        query={state.query}
                        goldenQueryId={state.goldenQueryId}
                        targetProductId={state.targetProductId}
                        activeFilterCount={countActiveFilters(state.filters)}
                        isFilterPanelOpen={isFilterPanelOpen}
                        goldenQueries={goldenQueries.data ?? []}
                        devices={devices.data ?? []}
                        onSubmitQuery={(query) => updateInputs({ query })}
                        onChooseGoldenQuery={handleChooseGoldenQuery}
                        onChooseDevice={(productId) => updateInputs({ targetProductId: productId })}
                        onToggleFilters={handleToggleFilters}
                    />
                    <PipelineStepper stage={stage} onChooseStage={(next) => update({ stage: next })} />
                    {caption}
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
                                    {(response) => (
                                        <ResultsTab
                                            response={response}
                                            taxonomy={taxonomy.data}
                                            targetProductId={state.targetProductId}
                                            applyConstraints={state.applyConstraints}
                                        />
                                    )}
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
            <FilterDrawer isOpen={!hasRoomForSidebar && isDrawerOpen} onOpenChange={setIsDrawerOpen}>
                {filterPanel}
            </FilterDrawer>
        </>
    );
}
