// The Decisions pages read the ADRs straight from docs/decisions at build time (ADR-0014 § Content): there is no
// copy to drift and no API endpoint. vite.config.ts allows the dev server to read outside the web-ui folder.

export interface Decision {
    /** The file name without `.md`, e.g. "0011-hybrid-search-rrf". Also the page's route. */
    id: string;
    number: string;
    title: string;
    /** The first word of the status line: Proposed, Accepted, Rejected, Superseded or Deprecated. */
    status: string;
    markdown: string;
}

// Only the numbered records: README, architecture and roadmap are working documents, and the pages never show them.
const adrFiles = import.meta.glob<string>('../../../../docs/decisions/[0-9][0-9][0-9][0-9]-*.md', {
    query: '?raw',
    import: 'default',
    eager: true,
});

export function parseDecision(id: string, markdown: string): Decision {
    const title = /^# ADR-\d{4}: (.+)$/m.exec(markdown)?.[1] ?? id;
    const statusLine = /^- \*\*Status:\*\* (.+)$/m.exec(markdown)?.[1] ?? '';
    const status = /^(Proposed|Accepted|Rejected|Superseded|Deprecated)\b/.exec(statusLine)?.[1] ?? 'Unknown';

    return { id, number: id.slice(0, 4), title, status, markdown };
}

/** Every ADR, in number order. README, architecture and roadmap are working documents, not decisions. */
export const decisions: readonly Decision[] = Object.entries(adrFiles)
    .map(([path, markdown]) => [path.replace(/^.*\/|\.md$/g, ''), markdown] as const)
    .filter(([id]) => /^\d{4}-/.test(id))
    .map(([id, markdown]) => parseDecision(id, markdown))
    .sort((a, b) => a.id.localeCompare(b.id));

export function findDecision(id: string | undefined): Decision | null {
    return decisions.find((decision) => decision.id === id) ?? null;
}

/**
 * Rewrites an ADR's relative links for the UI. A link to another ADR becomes `adr:`, which Markdown renders as
 * a link to its decision page (anchor kept). A link to any other repository file becomes `repo:`, shown as
 * text, because the UI doesn't serve those files. Web links and in-page anchors are left alone.
 */
export function rewriteDecisionLinks(markdown: string): string {
    return markdown.replace(/\]\((?![a-z][a-z0-9+.-]*:|#)([^)\s]+)\)/gi, (_, target: string) => {
        const [file = '', anchor] = target.split('#');
        const adrId = /^(?:\.\/)?(\d{4}-[a-z0-9-]+)\.md$/.exec(file)?.[1];

        if (adrId !== undefined) {
            return `](adr:${adrId}${anchor === undefined ? '' : `#${anchor}`})`;
        }

        const repositoryPath = new URL(file, 'https://repository.invalid/docs/decisions/').pathname.slice(1);
        return `](repo:${repositoryPath})`;
    });
}
