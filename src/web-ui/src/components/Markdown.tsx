import { isValidElement, useMemo, type ReactNode } from 'react';
import ReactMarkdown, { defaultUrlTransform, type Components } from 'react-markdown';
import { Link } from 'react-router';
import remarkGfm from 'remark-gfm';
import { GlossaryTerm } from '@/components/GlossaryTerm';
import { productLinkScheme } from '@/lib/citations';
import { slugify } from '@/lib/slug';
import { cn } from '@/lib/utils';

// Markdown — renders text from content/, docs/adr and the LLM (ADR-0014 § Content). Four link schemes are this repo's own:
//   term:     a glossary hover card               adr:  a link to a decision page
//   repo:     a repository file the UI doesn't serve, shown as text with its path on hover
//   product:  a citation in LLM text, rendered by the page (a chip that shows the product in the evidence set)
// A custom `a` renderer does it, with no remark plugin. Raw HTML in content is not rendered.

const ownSchemes = ['term:', 'adr:', 'repo:', productLinkScheme];

function renderLink(href: string | undefined, children: ReactNode): ReactNode {
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

    if (href?.startsWith('repo:')) {
        return (
            <span
                title={`${href.slice('repo:'.length)}: in the repository, not shown in the UI`}
                className="underline decoration-dashed underline-offset-4"
            >
                {children}
            </span>
        );
    }

    // A citation with no chip renderer (LLM text shown somewhere without an evidence set) stays plain text.
    if (href?.startsWith(productLinkScheme)) {
        return <span className="font-mono">{children}</span>;
    }

    return (
        <a href={href} className="underline underline-offset-4">
            {children}
        </a>
    );
}

const components: Components = {
    a: ({ href, children }) => renderLink(href, children),
    // Headings get GitHub-style ids, so links to a section of an ADR land on it.
    h1: ({ children }) => (
        <h1 id={slugify(textOf(children))} className="scroll-mt-4 text-3xl font-bold">
            {children}
        </h1>
    ),
    h2: ({ children }) => (
        <h2 id={slugify(textOf(children))} className="mt-4 scroll-mt-4 text-2xl font-bold">
            {children}
        </h2>
    ),
    h3: ({ children }) => (
        <h3 id={slugify(textOf(children))} className="mt-3 scroll-mt-4 text-xl font-bold">
            {children}
        </h3>
    ),
    h4: ({ children }) => (
        <h4 id={slugify(textOf(children))} className="mt-2 scroll-mt-4 text-lg font-bold">
            {children}
        </h4>
    ),
    code({ children }) {
        return <code className="rounded-md bg-muted px-1 py-0.5 font-mono text-[0.9em]">{children}</code>;
    },
    pre({ children }) {
        return (
            <pre className="overflow-x-auto rounded-xl bg-muted px-4 py-3 text-[0.85em] [&_code]:bg-transparent [&_code]:p-0">
                {children}
            </pre>
        );
    },
    blockquote({ children }) {
        return <blockquote className="border-l-4 pl-4 text-muted-foreground">{children}</blockquote>;
    },
    table({ children }) {
        return (
            <div className="overflow-x-auto">
                <table className="w-full border-collapse text-left [&_td]:border-t-2 [&_td]:px-2 [&_td]:py-1.5 [&_td]:align-top [&_th]:px-2 [&_th]:py-1.5 [&_th]:font-bold">
                    {children}
                </table>
            </div>
        );
    },
    ol({ children }) {
        return <ol className="flex flex-col gap-2 [counter-reset:step]">{children}</ol>;
    },
    // Numbers in circles, the shape language for steps (ADR-0014 § Visual design). The circle takes its colour
    // from --step-colour when the page sets one (the stage's triad colour), and the text colour otherwise.
    li({ children }) {
        return (
            <li className="relative pl-9 [counter-increment:step] before:absolute before:top-0 before:left-0 before:flex before:size-6 before:items-center before:justify-center before:rounded-full before:border-2 before:border-[color:var(--step-colour,currentColor)] before:text-sm before:font-bold before:text-[color:var(--step-colour,currentColor)] before:content-[counter(step)] [ul>&]:pl-0 [ul>&]:before:hidden">
                {children}
            </li>
        );
    },
    ul({ children }) {
        return <ul className="flex list-disc flex-col gap-1 pl-5">{children}</ul>;
    },
    hr() {
        return <hr className="border-t-2" />;
    },
};

// react-markdown blanks links with schemes it doesn't know, to stop `javascript:` URLs. Ours are safe.
function urlTransform(url: string): string {
    return ownSchemes.some((scheme) => url.startsWith(scheme)) ? url : defaultUrlTransform(url);
}

// The plain text of rendered children, for a heading's id.
function textOf(node: ReactNode): string {
    if (typeof node === 'string' || typeof node === 'number') {
        return String(node);
    }
    if (Array.isArray(node)) {
        return node.map(textOf).join('');
    }
    if (isValidElement(node)) {
        return textOf((node.props as { children?: ReactNode }).children);
    }
    return '';
}

export interface MarkdownProps {
    children: string;
    className?: string;
    /** Renders a `product:` citation link, e.g. as a chip; see `linkCitations` in lib/citations.ts. */
    renderProductLink?: (productId: string) => ReactNode;
}

export function Markdown({ children, className, renderProductLink }: MarkdownProps) {
    const allComponents = useMemo<Components>(
        () =>
            renderProductLink === undefined
                ? components
                : {
                      ...components,
                      a: ({ href, children: linkChildren }) =>
                          href?.startsWith(productLinkScheme)
                              ? renderProductLink(href.slice(productLinkScheme.length))
                              : renderLink(href, linkChildren),
                  },
        [renderProductLink],
    );

    return (
        <div className={cn('flex flex-col gap-2', className)}>
            <ReactMarkdown remarkPlugins={[remarkGfm]} urlTransform={urlTransform} components={allComponents}>
                {children}
            </ReactMarkdown>
        </div>
    );
}
