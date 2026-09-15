import { Presentation } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePresentation } from '@/hooks/usePresentation';
import { cn } from '@/lib/utils';

/** The header's presentation-mode switch: larger type and fewer controls, for a projector. */
export function PresentationToggle() {
    const { isPresentationMode, setPresentationMode } = usePresentation();

    return (
        <Button
            variant="outline"
            aria-label="Presentation mode"
            aria-pressed={isPresentationMode}
            title={isPresentationMode ? 'Presentation mode is on' : 'Presentation mode is off'}
            onClick={() => setPresentationMode(!isPresentationMode)}
            className={cn(
                'rounded-full border-2 font-bold',
                isPresentationMode &&
                    'border-ontology bg-ontology-tint text-ontology-ink dark:bg-ontology-tint',
            )}
        >
            <Presentation aria-hidden="true" />
            <span className="hidden lg:inline">Presentation</span>
        </Button>
    );
}
