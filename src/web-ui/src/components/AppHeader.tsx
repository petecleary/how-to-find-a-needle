import { Logo } from '@/components/Logo';
import { ThemeToggle } from '@/components/ThemeToggle';

/** The bar across the top of every page: the Pi & Mash logo, the talk title and the theme switch. */
export function AppHeader() {
    return (
        <header className="flex h-14 flex-none items-center gap-3 border-b-2 bg-card px-6">
            <Logo size={32} />
            <span className="font-brand text-2xl font-extrabold">How to Find a Needle</span>
            <div className="flex-1" />
            {/* TODO(Phase 3): page navigation arrives with the pages (step 10), the presentation toggle in step 11. */}
            <ThemeToggle />
        </header>
    );
}
