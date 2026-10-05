import { KeyRound } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useModels } from '@/hooks/useModels';

/**
 * The header's way into Models and API keys (ADR-0019). Hidden in presentation mode, like the page links: on stage the
 * Stage 6–7 picker has its own button.
 */
export function ModelSettingsButton() {
    const { setSettingsOpen } = useModels();

    return (
        <Button
            variant="outline"
            size="icon"
            className="rounded-full border-2 presentation:hidden"
            onClick={() => setSettingsOpen(true)}
            aria-label="Models and API keys"
            title="Models and API keys"
        >
            <KeyRound aria-hidden="true" />
        </Button>
    );
}
