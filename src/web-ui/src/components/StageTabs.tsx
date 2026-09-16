import { BookOpen, List, Settings, Sparkles, type LucideIcon } from 'lucide-react';
import { Tabs as TabsPrimitive } from 'radix-ui';
import { useEffect, type ReactNode } from 'react';
import { isTypingTarget } from '@/lib/keyboard';
import type { StageTab } from '@/lib/searchState';
import { stageColourClasses, type PipelineStage } from '@/lib/stageGroup';
import { isTabAvailable, stageTabDefinitions, tabForShortcut } from '@/lib/stageTabs';
import { cn } from '@/lib/utils';

// StageTabs — a stage split into How it works · Results · Answer · Under the hood, so the presenter can
// step through a stage or jump to what a question needs (ADR-0014 § Stage screen). H / R / A / U jump to
// a tab from anywhere on the page, except while typing. Built on Radix Tabs for the ARIA tablist.

const tabIcons: Record<StageTab, LucideIcon> = {
    'how-it-works': BookOpen,
    results: List,
    answer: Sparkles,
    'under-the-hood': Settings,
};

export interface StageTabsProps {
    stage: PipelineStage;
    tab: StageTab;
    onChooseTab: (tab: StageTab) => void;
    /** Small labels beside a tab's name, e.g. `{ results: '50', 'under-the-hood': '8 steps' }`. */
    counts: Partial<Record<StageTab, string>>;
    /** The stage's switches, on the right of the tab row, next to what they change. */
    options?: ReactNode;
    panels: Record<StageTab, ReactNode>;
}

export function StageTabs({ stage, tab, onChooseTab, counts, options, panels }: StageTabsProps) {
    useEffect(() => {
        function handleKeyDown(event: KeyboardEvent) {
            if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey) {
                return;
            }
            // Letters typed into the query box, or used to search a dropdown's options, must not switch tabs.
            if (isTypingTarget(event.target)) {
                return;
            }

            const shortcutTab = tabForShortcut(event.key);
            if (shortcutTab !== null && isTabAvailable(shortcutTab, stage)) {
                event.preventDefault();
                onChooseTab(shortcutTab);
            }
        }

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [onChooseTab, stage]);

    const colours = stageColourClasses(stage);

    function handleValueChange(value: string) {
        const next = stageTabDefinitions.find((definition) => definition.tab === value);
        if (next !== undefined) {
            onChooseTab(next.tab);
        }
    }

    return (
        <TabsPrimitive.Root
            value={tab}
            onValueChange={handleValueChange}
            className="flex flex-1 flex-col gap-3"
        >
            <div className="flex flex-wrap items-end gap-x-4 border-b-2">
                <TabsPrimitive.List aria-label="Stage sections" className="flex gap-5">
                    {stageTabDefinitions.map(({ tab: candidate, label, shortcut }) => {
                        const Icon = tabIcons[candidate];
                        const isAvailable = isTabAvailable(candidate, stage);
                        const count = counts[candidate];

                        return (
                            <TabsPrimitive.Trigger
                                key={candidate}
                                value={candidate}
                                disabled={!isAvailable}
                                aria-keyshortcuts={shortcut.toUpperCase()}
                                className={cn(
                                    '-mb-0.5 flex items-center gap-[7px] border-b-4 border-transparent px-1 pt-2 pb-1.5 text-lg font-bold whitespace-nowrap text-muted-foreground hover:text-foreground disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:text-muted-foreground data-[state=active]:text-foreground',
                                    candidate === tab && colours.border,
                                )}
                            >
                                <Icon aria-hidden="true" className="size-[18px]" />
                                {label}
                                {isAvailable ? null : <span className="text-xs font-normal">Stages 6–7</span>}
                                {isAvailable && count !== undefined ? (
                                    <span className="rounded-full bg-muted px-2 text-[13px] font-normal text-muted-foreground">
                                        {count}
                                    </span>
                                ) : null}
                            </TabsPrimitive.Trigger>
                        );
                    })}
                </TabsPrimitive.List>
                <div className="ml-auto flex items-center gap-4 pb-1.5">{options}</div>
            </div>

            {stageTabDefinitions.map(({ tab: candidate }) => (
                <TabsPrimitive.Content key={candidate} value={candidate} className="flex flex-1 flex-col">
                    {panels[candidate]}
                </TabsPrimitive.Content>
            ))}
        </TabsPrimitive.Root>
    );
}
