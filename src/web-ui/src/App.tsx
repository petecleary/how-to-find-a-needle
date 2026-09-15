import { Navigate, Route, Routes } from 'react-router';
import { DemoPage } from '@/pages/DemoPage';

// TODO(Phase 3): step 10 adds Home at "/", talk mode, the glossary and the decisions pages.

export function App() {
    return (
        <Routes>
            <Route path="/demo" element={<DemoPage />} />
            <Route path="*" element={<Navigate to="/demo" replace />} />
        </Routes>
    );
}
