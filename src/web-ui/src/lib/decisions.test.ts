import { describe, expect, it } from 'vitest';
import { adrLinkIds } from './content';
import { decisions, findDecision, parseDecision, rewriteDecisionLinks } from './decisions';
import { slugify } from './slug';

describe('decisions', () => {
    it('lists every ADR in order, each with a title and a known status', () => {
        expect(decisions.map((decision) => decision.number)).toEqual(
            Array.from({ length: decisions.length }, (_, index) => String(index + 1).padStart(4, '0')),
        );
        expect(decisions.length).toBeGreaterThanOrEqual(18);
        expect(
            decisions.filter((decision) => decision.status === 'Unknown' || decision.title === decision.id),
        ).toEqual([]);
    });

    it('reads the title and the first word of the status line', () => {
        const decision = parseDecision(
            '0012-bge-m3-dense-and-sparse',
            '# ADR-0012: BGE-M3 dense + sparse (rejected)\n\n- **Status:** Rejected (2026-09-14) by [ADR-0018](x.md)',
        );

        expect(decision).toMatchObject({
            number: '0012',
            title: 'BGE-M3 dense + sparse (rejected)',
            status: 'Rejected',
        });
    });
});

describe('rewriteDecisionLinks', () => {
    it('turns ADR links into decision pages, other files into repository paths, and leaves web links alone', () => {
        const markdown = [
            '[ADR-0013](0013-domain-ontology-and-compatibility.md#tests)',
            '[roadmap](roadmap.md#phase-1--data)',
            '[design](../design/README.md)',
            '[SKOS](https://www.w3.org/TR/skos-reference/)',
            '[below](#consequences)',
        ].join(' ');

        expect(rewriteDecisionLinks(markdown)).toBe(
            [
                '[ADR-0013](adr:0013-domain-ontology-and-compatibility#tests)',
                '[roadmap](repo:docs/adr/roadmap.md)',
                '[design](repo:docs/design/README.md)',
                '[SKOS](https://www.w3.org/TR/skos-reference/)',
                '[below](#consequences)',
            ].join(' '),
        );
    });

    it('leaves no link between ADRs that points to a missing ADR or heading', () => {
        const broken = decisions.flatMap((decision) =>
            adrLinkIds(rewriteDecisionLinks(decision.markdown)).flatMap((link) => {
                const [id, anchor] = link.split('#');
                const target = findDecision(id);
                if (target === null) {
                    return [`${decision.number} → ${link}`];
                }

                const headings = [...target.markdown.matchAll(/^#{1,6} (.+)$/gm)].map((match) =>
                    slugify(match[1] ?? ''),
                );
                return anchor === undefined || headings.includes(anchor)
                    ? []
                    : [`${decision.number} → ${link}`];
            }),
        );

        expect(broken).toEqual([]);
    });
});

describe('slugify', () => {
    it('makes GitHub-style anchors', () => {
        expect(slugify('Visual design')).toBe('visual-design');
        expect(slugify('Stage 1 — Structured search')).toBe('stage-1--structured-search');
        expect(slugify('`GET /api/taxonomy`')).toBe('get-apitaxonomy');
    });
});
