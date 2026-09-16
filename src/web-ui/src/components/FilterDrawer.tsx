import type { ReactNode } from 'react';
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/components/ui/sheet';

export interface FilterDrawerProps {
    isOpen: boolean;
    onOpenChange: (isOpen: boolean) => void;
    /** The FilterPanel, which carries its own visible heading. */
    children: ReactNode;
}

/**
 * The filters as a drawer over the page, opened from the Filters button: used in talk mode, where the
 * results need the full width, and on screens too narrow for the sidebar (ADR-0014).
 */
export function FilterDrawer({ isOpen, onOpenChange, children }: FilterDrawerProps) {
    return (
        <Sheet open={isOpen} onOpenChange={onOpenChange}>
            <SheetContent side="right" className="w-80 overflow-y-auto border-l-2 px-5 py-4 sm:max-w-sm">
                <SheetTitle className="sr-only">Filters</SheetTitle>
                <SheetDescription className="sr-only">
                    Brand, price, category and spec filters, applied before ranking in every stage.
                </SheetDescription>
                {children}
            </SheetContent>
        </Sheet>
    );
}
