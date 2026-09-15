import ReactMarkdown, { defaultUrlTransform, type Components } from 'react-markdown';
import { Link } from 'react-router';
import remarkGfm from 'remark-gfm';
import { GlossaryTerm } from '@/components/GlossaryTerm';
import { cn } from '@/lib/utils';

// Markdown — renders text from content/ (ADR-0014 § Content). Two link schemes are this repo's own:
// `term:` becomes a glossary hover card and `adr:` a link to the decision page. A custom `a` renderer does it,
// with no remark plugin. Raw HTML in content is not rendered, so content can't inject markup.

const components: Components = {
    a({ href, children }) {
        if (href?.startsWith('term:')) {
            return <GlossaryTerm id={href.slice('term:'.length)}>{children}</GlossaryTerm>;
        }

        if (href?.startsWith('adr:')) {
            return (
                <Link
                    to={`/decisions/${href.slice('adr:'.length)}`}
                    className="font-bold underline underline-offset-4"
                >
                    {children}
                </Link>
            );
        }

        return (
            <a href={href} className="underline underline-offset-4">
                {children}
            </a>
        );
    },
    code({ children }) {
        return <code className="rounded-md bg-muted px-1 py-0.5 font-mono text-[0.9em]">{children}</code>;
    },
    ol({ children }) {
        return <ol className="flex flex-col gap-2 [counter-reset:step]">{children}</ol>;
    },
    // Numbers in circles, the shape language for steps (ADR-0014 § Visual design). The circle takes its colour
    // from --step-colour when the page sets one (the stage's triad colour), and the text colour otherwise.
    li({ children }) {
        return (
            <li className="relative pl-9 [counter-increment:step] before:absolute before:top-0 before:left-0 before:flex before:size-6 before:items-center before:justify-center before:rounded-full before:border-2 before:border-[color:var(--step-colour,currentColor)] before:text-sm before:font-bold before:text-[color:var(--step-colour,currentColor)] before:content-[counter(step)]">
                {children}
            </li>
        );
    },
    ul({ children }) {
        return <ul className="flex list-disc flex-col gap-1 pl-5">{children}</ul>;
    },
};

// react-markdown blanks links with schemes it doesn't know, to stop `javascript:` URLs. Ours are safe.
function urlTransform(url: string): string {
    return url.startsWith('term:') || url.startsWith('adr:') ? url : defaultUrlTransform(url);
}

export interface MarkdownProps {
    children: string;
    className?: string;
}

export function Markdown({ children, className }: MarkdownProps) {
    return (
        <div className={cn('flex flex-col gap-2', className)}>
            <ReactMarkdown remarkPlugins={[remarkGfm]} urlTransform={urlTransform} components={components}>
                {children}
            </ReactMarkdown>
        </div>
    );
}
