import { describe, expect, it } from 'vitest';
import type { SearchResponse } from '@/api/client';
import { gq01OntologyResponse } from '@/test/responses';
import { operatorPrefix, readRuleChecks, readWantedConcepts } from './traceDetails';

describe('readRuleChecks', () => {
    it('reads every check for the 45W barrel charger, with the values compared', () => {
        const checks = readRuleChecks(gq01OntologyResponse)?.filter(
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
            ...gq01OntologyResponse,
            debugTrace: {
                ...gq01OntologyResponse.debugTrace,
                steps: gq01OntologyResponse.debugTrace.steps.filter(
                    (step) => !step.title.startsWith('Constrain'),
                ),
            },
        };

        expect(readRuleChecks(withoutConstrain)).toBeNull();
    });
});

describe('readWantedConcepts', () => {
    it('reads the concepts the query asked for', () => {
        expect(readWantedConcepts(gq01OntologyResponse)).toEqual(['chargers']);
    });
});

describe('operatorPrefix', () => {
    it('reads as the device side of "has … needs …"', () => {
        expect(operatorPrefix('greaterOrEqual')).toBe('≥ ');
        expect(operatorPrefix('equals')).toBe('');
    });
});
