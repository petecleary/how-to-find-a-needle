import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { PresentationProvider } from '@/components/PresentationProvider';
import { talkStartPath } from '@/lib/talk';
import { DemoPage } from '@/pages/DemoPage';
import { HomePage } from '@/pages/HomePage';
import { TalkPage } from '@/pages/TalkPage';

// The reading pages load on first visit. They carry every ADR's text, which the talk and the demo don't need,
// so the page the audience sees first stays small.
const GlossaryPage = lazy(() =>
    import('@/pages/GlossaryPage').then((module) => ({ default: module.GlossaryPage })),
);
const DecisionsPage = lazy(() =>
    import('@/pages/DecisionsPage').then((module) => ({ default: module.DecisionsPage })),
);
const DecisionPage = lazy(() =>
    import('@/pages/DecisionPage').then((module) => ({ default: module.DecisionPage })),
);

// The pages (ADR-0014 § Pages and routes). Talk position lives in the route; the demo's state in the query string.
export function App() {
    return (
        <PresentationProvider>
            <Suspense fallback={<p className="px-6 py-8 text-muted-foreground">Loading…</p>}>
                <Routes>
                    <Route path="/" element={<HomePage />} />
                    <Route path="/talk" element={<Navigate to={talkStartPath()} replace />} />
                    <Route path="/talk/:step/:tab?" element={<TalkPage />} />
                    <Route path="/demo" element={<DemoPage />} />
                    <Route path="/glossary" element={<GlossaryPage />} />
                    <Route path="/decisions" element={<DecisionsPage />} />
                    <Route path="/decisions/:id" element={<DecisionPage />} />
                    <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
            </Suspense>
        </PresentationProvider>
    );
}
