import { useEffect } from 'react';
import { useNavigate } from 'react-router';
import { isTypingTarget } from '@/lib/keyboard';

/**
 * ← / → (and Page Up / Page Down, which presentation clickers send) move through the talk. A key another control
 * already handled is left alone: the stage stepper and the tab lists use ← / → themselves when focused, and call
 * preventDefault, so moving between stages or tabs never also changes the talk step.
 */
export function useTalkKeys(nextPath: string | null, previousPath: string | null): void {
    const navigate = useNavigate();

    useEffect(() => {
        function handleKeyDown(event: KeyboardEvent) {
            if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
                return;
            }
            if (isTypingTarget(event.target)) {
                return;
            }

            const path =
                event.key === 'ArrowRight' || event.key === 'PageDown'
                    ? nextPath
                    : event.key === 'ArrowLeft' || event.key === 'PageUp'
                      ? previousPath
                      : null;

            if (path !== null) {
                event.preventDefault();
                navigate(path);
            }
        }

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [navigate, nextPath, previousPath]);
}
