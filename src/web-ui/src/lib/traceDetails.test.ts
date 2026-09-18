import { describe, expect, it } from 'vitest';
import type { SearchResponse } from '@/api/client';
import { gq03OntologyResponse } from '@/test/responses';
import {
    operatorPrefix,
    parseRrfFormula,
    readExpansionDetails,
    readRuleChecks,
    readWantedConcepts,
} from './traceDetails';

describe('readRuleChecks', () => {
    it('reads every check for the 45W barrel charger, with the values compared', () => {
        const checks = readRuleChecks(gq03OntologyResponse)?.filter(
            (check) => check.candidateId === 'PROD-0014',
        );

        expect(checks).toEqual([
            expect.objectContaining({
                accessorySpec: 'wattageW',
                accessoryValue: '45W',
                operator: 'greaterOrEqual',
                deviceValue: '65W',
                result: 'Fail',
            }),
            expect.objectContaining({
                accessorySpec: 'connector',
                accessoryValue: '5.5mm barrel',
                operator: 'equals',
                deviceValue: 'USB-C',
                result: 'Fail',
            }),
        ]);
    });

    it('returns null when no rules were checked', () => {
        const withoutConstrain: SearchResponse = {
            ...gq03OntologyResponse,
            debugTrace: {
                ...gq03OntologyResponse.debugTrace,
                steps: gq03OntologyResponse.debugTrace.steps.filter(
                    (step) => !step.title.startsWith('Constrain'),
                ),
            },
        };

        expect(readRuleChecks(withoutConstrain)).toBeNull();
    });
});

describe('readWantedConcepts', () => {
    it('reads the concepts the query asked for', () => {
        expect(readWantedConcepts(gq03OntologyResponse)).toEqual(['chargers']);
    });
});

describe('parseRrfFormula', () => {
    it("splits the API's formula text without recomputing it", () => {
        expect(parseRrfFormula('PROD-0012: 1/(60+3) + 1/(60+1) = 0.03227')).toEqual({
            id: 'PROD-0012',
            expression: '1/(60+3) + 1/(60+1)',
            total: '0.03227',
        });
        expect(parseRrfFormula('not a formula')).toBeNull();
    });
});

describe('readExpansionDetails', () => {
    it('reads an expansion that was switched off as empty lists, not a crash', () => {
        const details = readExpansionDetails({
            expandSynonyms: false,
            phrases: null,
            keywordOrGroups: null,
            keywordRemainingText: null,
            embeddingText: 'power adapter for my laptop',
        });

        expect(details).toEqual({
            expandSynonyms: false,
            phrases: [],
            keywordOrGroups: [],
            keywordRemainingText: null,
            embeddingText: 'power adapter for my laptop',
        });
    });
});

describe('operatorPrefix', () => {
    it('reads as the device side of "has … needs …"', () => {
        expect(operatorPrefix('greaterOrEqual')).toBe('≥ ');
        expect(operatorPrefix('equals')).toBe('');
    });
});
