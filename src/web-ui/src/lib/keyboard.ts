/**
 * True when a key press belongs to what the user is typing into: a text box, a select, or a dropdown being
 * searched by letter. Page-wide shortcuts (H / R / A / U, and ← / → in talk mode) must ignore those keys.
 */
export function isTypingTarget(target: EventTarget | null): boolean {
    if (!(target instanceof HTMLElement)) {
        return false;
    }

    const isTextField = ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName);
    return (
        isTextField ||
        target.isContentEditable ||
        target.closest('[role="combobox"], [role="listbox"]') !== null
    );
}
