import { Link, NavLink } from 'react-router';
import { Logo } from '@/components/Logo';
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

/** The bar across the top of every page: the logo and title (home), the talk position, the pages and the theme. */
export function AppHeader({ position, wordmark = 'talk' }: AppHeaderProps) {
    return (
        <header className="flex h-14 flex-none items-center gap-3 border-b-2 bg-card px-6">
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
            <nav aria-label="Pages">
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
            {/* TODO(Phase 3): step 11 adds the presentation-mode toggle here. */}
            <ThemeToggle />
        </header>
    );
}
