import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router';
import { App } from './App';
import { ThemeProvider } from './components/ThemeProvider';
import './index.css';

const rootElement = document.getElementById('root');
if (rootElement === null) {
    throw new Error('index.html has no #root element to render into.');
}

createRoot(rootElement).render(
    <StrictMode>
        <BrowserRouter>
            <ThemeProvider>
                <App />
            </ThemeProvider>
        </BrowserRouter>
    </StrictMode>,
);
