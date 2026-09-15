import { Funnel, Laptop, Search } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import type { DemoDevice, GoldenQuery } from '@/api/client';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { cn } from '@/lib/utils';

// Radix Select can't use an empty string as an item's value, so this stands for "nothing chosen".
const noChoice = 'none';

const pillTriggerClass =
    'rounded-full border-2 bg-card px-3 py-1.5 text-[15px] font-bold shadow-none data-[size=default]:h-auto dark:bg-card dark:hover:bg-muted';

export interface SearchBarProps {
    query: string;
    goldenQueryId: string | null;
    targetProductId: string | null;
    activeFilterCount: number;
    /** Whether the filter sidebar or drawer is showing, for the Filters button's `aria-expanded`. */
    isFilterPanelOpen: boolean;
    goldenQueries: GoldenQuery[];
    devices: DemoDevice[];
    onSubmitQuery: (query: string) => void;
    onChooseGoldenQuery: (goldenQuery: GoldenQuery | null) => void;
    onChooseDevice: (productId: string | null) => void;
    onToggleFilters: () => void;
}

/**
 * The inputs every stage shares: a golden-query preset, the query, the device the shopper owns, and the
 * filters. Changing any of them sends the same new request to the current stage.
 */
export function SearchBar({
    query,
    goldenQueryId,
    targetProductId,
    activeFilterCount,
    isFilterPanelOpen,
    goldenQueries,
    devices,
    onSubmitQuery,
    onChooseGoldenQuery,
    onChooseDevice,
    onToggleFilters,
}: SearchBarProps) {
    // Typing edits a draft; the search runs on Enter, not on every keystroke. When the query changes
    // from outside (a golden query was chosen, or Back was pressed), the draft follows it.
    const [draft, setDraft] = useState(query);
    const [lastQuery, setLastQuery] = useState(query);
    if (query !== lastQuery) {
        setLastQuery(query);
        setDraft(query);
    }

    const device = devices.find((candidate) => candidate.id === targetProductId);

    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        onSubmitQuery(draft);
    }

    return (
        <div className="flex flex-wrap items-center gap-2.5">
            <Select
                value={goldenQueryId ?? noChoice}
                onValueChange={(value) =>
                    onChooseGoldenQuery(goldenQueries.find((goldenQuery) => goldenQuery.id === value) ?? null)
                }
            >
                <SelectTrigger
                    aria-label="Golden query"
                    className={cn(
                        pillTriggerClass,
                        'font-mono text-sm',
                        goldenQueryId && 'text-ontology-ink',
                    )}
                >
                    <SelectValue>{goldenQueryId ?? 'Preset'}</SelectValue>
                </SelectTrigger>
                <SelectContent position="popper" align="start">
                    <SelectItem value={noChoice}>No preset</SelectItem>
                    {goldenQueries.map((goldenQuery) => (
                        <SelectItem key={goldenQuery.id} value={goldenQuery.id}>
                            <span className="font-mono text-ontology-ink">{goldenQuery.id}</span>
                            {goldenQuery.title}
                        </SelectItem>
                    ))}
                </SelectContent>
            </Select>

            <form
                role="search"
                onSubmit={handleSubmit}
                className="flex min-w-72 flex-1 items-center gap-2.5 rounded-full border-2 bg-card px-4 py-1.5 focus-within:border-ring focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-ring"
            >
                <Search aria-hidden="true" className="size-5 flex-none text-muted-foreground" />
                <input
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    aria-label="Search query"
                    placeholder="Search the catalogue, then press Enter"
                    className="min-w-0 flex-1 bg-transparent text-xl font-bold outline-none placeholder:font-normal placeholder:text-muted-foreground"
                />
            </form>

            <Select
                value={targetProductId ?? noChoice}
                onValueChange={(value) => onChooseDevice(value === noChoice ? null : value)}
            >
                <SelectTrigger aria-label="Target device" className={pillTriggerClass}>
                    <Laptop aria-hidden="true" className="text-muted-foreground" />
                    <SelectValue>
                        {targetProductId === null ? (
                            <span className="font-normal text-muted-foreground">No device</span>
                        ) : (
                            <>
                                <span className="font-normal text-muted-foreground">I own</span>
                                {device?.name ?? targetProductId}
                            </>
                        )}
                    </SelectValue>
                </SelectTrigger>
                <SelectContent position="popper" align="end">
                    <SelectItem value={noChoice}>No device</SelectItem>
                    {devices.map((candidate) => (
                        <SelectItem key={candidate.id} value={candidate.id}>
                            {candidate.name}
                            <span className="text-muted-foreground">{candidate.brand}</span>
                        </SelectItem>
                    ))}
                </SelectContent>
            </Select>

            <Button
                type="button"
                variant="outline"
                aria-expanded={isFilterPanelOpen}
                onClick={onToggleFilters}
                className={cn(
                    'h-auto rounded-full border-2 bg-card px-3 py-1.5 text-[15px] font-bold shadow-none',
                    isFilterPanelOpen && 'border-ontology',
                )}
            >
                <Funnel aria-hidden="true" className="text-muted-foreground" />
                Filters
                {activeFilterCount > 0 ? (
                    <span className="rounded-full bg-ontology px-1.5 text-[13px] text-on-ontology">
                        {activeFilterCount}
                    </span>
                ) : (
                    <span className="text-sm font-normal text-muted-foreground">none</span>
                )}
            </Button>
        </div>
    );
}
