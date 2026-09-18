import { ArrowLeft } from 'lucide-react';
import { useEffect } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router';
import { AppHeader } from '@/components/AppHeader';
import { Markdown } from '@/components/Markdown';
import { findDecision, rewriteDecisionLinks } from '@/lib/decisions';

/** `/decisions/:id`: one ADR, rendered from docs/decisions, with its links to other ADRs pointing at their pages. */
export function DecisionPage() {
    const { id } = useParams();
    const { hash } = useLocation();
    const decision = findDecision(id);
    const navigate = useNavigate();

    // A link to a section (#visual-design) scrolls to its heading; otherwise the page opens at the top.
    useEffect(() => {
        if (hash === '') {
            window.scrollTo(0, 0);
        } else {
            document.getElementById(decodeURIComponent(hash.slice(1)))?.scrollIntoView();
        }
    }, [id, hash]);

    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader />
            <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-4 px-6 py-8">
                <div className="flex items-center justify-between">
                    <a
                        href="/decisions"
                        onClick={(event) => {
                            event.preventDefault();
                            navigate(-1);
                        }}
                        className="flex items-center gap-1.5 text-muted-foreground hover:underline"
                    >
                        <ArrowLeft aria-hidden="true" className="size-4" /> Back
                    </a>

                    <Link to="/decisions" className="text-muted-foreground hover:underline">
                        All decisions
                    </Link>
                </div>
                {decision === null ? (
                    <p role="alert" className="text-lg">
                        There is no decision record called “{id}”.
                    </p>
                ) : (
                    <Markdown className="gap-3 text-[17px] leading-relaxed">
                        {rewriteDecisionLinks(decision.markdown)}
                    </Markdown>
                )}
            </main>
        </div>
    );
}
