// StreamingBadge — "streaming" beside a panel while its text is still arriving: the text shown hasn't been validated yet.

export interface StreamingBadgeProps {
    label?: string;
}

export function StreamingBadge({ label = 'streaming' }: StreamingBadgeProps) {
    return (
        <span role="status" className="inline-flex items-center gap-1.5 text-sm font-bold text-pedagogy-ink">
            <span aria-hidden="true" className="size-2 animate-pulse rounded-full bg-pedagogy" />
            {label}
        </span>
    );
}
