export interface ProductCellProps {
    id: string;
    productNames: Map<string, string>;
}

/** A product in a trace table: its name when the response has it, and always its ID. */
export function ProductCell({ id, productNames }: ProductCellProps) {
    const name = productNames.get(id);

    return (
        <span className="flex flex-wrap items-baseline gap-x-1.5">
            {name === undefined ? null : <b>{name}</b>}
            <span className="font-mono text-xs text-muted-foreground">{id}</span>
        </span>
    );
}
