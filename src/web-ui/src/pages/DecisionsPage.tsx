import { Link } from 'react-router';
import { AppHeader } from '@/components/AppHeader';
import { decisions } from '@/lib/decisions';
import { cn } from '@/lib/utils';

const statusClasses: Record<string, string> = {
    Accepted: 'bg-compatible-tint text-compatible-ink',
    Rejected: 'bg-incompatible-tint text-incompatible-ink',
    Proposed: 'bg-muted text-muted-foreground',
};

/** `/decisions`: every architecture decision record, read from docs/adr (ADR-0014 § Pages). */
export function DecisionsPage() {
    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader />
            <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-6 px-6 py-8">
                <div className="flex flex-col gap-2">
                    <h1 className="text-4xl font-bold">Decisions</h1>
                    <p className="text-lg text-muted-foreground">
                        Why the repository is built the way it is. Each record gives the context, the
                        decision, the alternatives considered and what to teach from it.
                    </p>
                </div>
                <ol className="flex flex-col gap-2">
                    {decisions.map((decision) => (
                        <li key={decision.id}>
                            <Link
                                to={`/decisions/${decision.id}`}
                                className="flex items-center gap-4 rounded-card border-2 bg-card px-5 py-3 hover:border-ring"
                            >
                                <span className="font-mono text-muted-foreground">{decision.number}</span>
                                <b className="flex-1 text-lg">{decision.title}</b>
                                <span
                                    className={cn(
                                        'rounded-full px-2.5 py-0.5 text-sm font-bold',
                                        statusClasses[decision.status] ?? 'bg-muted text-muted-foreground',
                                    )}
                                >
                                    {decision.status}
                                </span>
                            </Link>
                        </li>
                    ))}
                </ol>
            </main>
        </div>
    );
}
