import { useState, type ComponentProps } from 'react';
import { Input } from '@/components/ui/input';

export interface CommittedInputProps extends Omit<ComponentProps<'input'>, 'value' | 'onChange' | 'onBlur'> {
    value: string;
    /** Called on Enter or when the box loses focus, and only if the text changed. */
    onCommit: (text: string) => void;
}

/**
 * A text box that applies its value when you finish typing, not on every keystroke. Every filter change is
 * a new search and a new URL entry, so typing "Brakk" shouldn't search for "B", "Br" and "Bra" first.
 */
export function CommittedInput({ value, onCommit, onKeyDown, ...props }: CommittedInputProps) {
    const [draft, setDraft] = useState(value);
    const [lastValue, setLastValue] = useState(value);
    if (value !== lastValue) {
        setLastValue(value);
        setDraft(value);
    }

    function commit() {
        if (draft === value) {
            return;
        }

        onCommit(draft);
        // If the parent normalises the text back to the same value ("  " → no brand), show what applies.
        setDraft(value);
    }

    return (
        <Input
            {...props}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onBlur={commit}
            onKeyDown={(event) => {
                onKeyDown?.(event);
                if (event.key === 'Enter') {
                    commit();
                }
            }}
        />
    );
}
