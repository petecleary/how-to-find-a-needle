import { Link, NavLink } from 'react-router';
import { Logo } from '@/components/Logo';
import { PresentationToggle } from '@/components/PresentationToggle';
import { ThemeToggle } from '@/components/ThemeToggle';
import { cn } from '@/lib/utils';

const pages = [
    { to: '/talk', label: 'Talk' },
    { to: '/demo', label: 'Demo' },
    { to: '/glossary', label: 'Glossary' },
    { to: '/decisions', label: 'Decisions' },
];

export interface AppHeaderProps {
    /** Where the presenter is, beside the title, e.g. "Stage 5 of 7". */
    position?: string;
    /** The talk title on most pages; the company name on Home, where the title is the page's heading. */
    wordmark?: 'talk' | 'company';
}

// Keyboard users land on the page's content without tabbing through the header on every page.
function skipToContent() {
    const main = document.querySelector('main');
    if (main !== null) {
        main.tabIndex = -1;
        main.focus();
    }
}

/**
 * The bar across the top of every page: logo and title (home), the talk position, the pages, and the
 * presentation and theme switches. In presentation mode the page links are hidden: the talk moves by keyboard.
 */
export function AppHeader({ position, wordmark = 'talk' }: AppHeaderProps) {
    return (
        <header className="relative flex h-14 flex-none items-center gap-3 border-b-2 bg-card px-6">
            <button
                type="button"
                onClick={skipToContent}
                className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-full focus:border-2 focus:bg-card focus:px-4 focus:py-2 focus:font-bold"
            >
                Skip to content
            </button>
            <Link to="/" className="flex items-center gap-3">
                <Logo size={32} />
                <span className="hidden font-brand text-2xl font-extrabold sm:inline">
                    {wordmark === 'talk' ? 'How to Find a Needle' : 'Pi & Mash'}
                </span>
            </Link>
            {position === undefined ? null : (
                <span className="hidden text-lg text-muted-foreground md:inline">/ {position}</span>
            )}
            <div className="flex-1" />
            <nav aria-label="Pages" className="presentation:hidden">
                <ul className="flex items-center gap-1">
                    {pages.map((page) => (
                        <li key={page.to}>
                            <NavLink
                                to={page.to}
                                className={({ isActive }) =>
                                    cn(
                                        'rounded-full px-3 py-1 text-[17px] font-bold hover:bg-muted',
                                        isActive ? 'text-ontology-ink' : 'text-foreground',
                                    )
                                }
                            >
                                {page.label}
                            </NavLink>
                        </li>
                    ))}
                </ul>
            </nav>
            <PresentationToggle />
            <ThemeToggle />
        </header>
    );
}
