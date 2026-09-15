import type { SearchResponse } from '@/api/client';

// Reading a trace step's `details`. The contract types `details` as an open dictionary (ADR-0003): each
// stage writes its own keys. These readers check every field at runtime, so a trace that changes shape
// shows up as missing data rather than a crash.

export type CheckResult = 'Pass' | 'Fail' | 'Unknown';

/** One check of one domain rule against one candidate, from Stage 5's Constrain step (ADR-0013). */
export interface RuleCheck {
    candidateId: string;
    /** The accessory and device types the rule links, e.g. "chargers → laptops". */
    rule: string;
    /** The check's definition from the TTL, e.g. "The charger's plug must fit the laptop's charging port." */
    definition: string;
    accessorySpec: string;
    /** The candidate's value, or `null` when the product doesn't have the spec (the check is Unknown). */
    accessoryValue: string | null;
    operator: string;
    deviceSpec: string;
    deviceValue: string | null;
    result: CheckResult;
}

/** The `details` value under `key` in the first trace step that has it, or `undefined`. */
export function findTraceDetail(response: SearchResponse, key: string): unknown {
    for (const step of response.debugTrace.steps) {
        const { details } = step;
        if (details != null && Object.hasOwn(details, key)) {
            return details[key];
        }
    }

    return undefined;
}

/**
 * Every rule check Stage 5 ran, failed and passed. `null` when no checks were run: another stage, or
 * Stage 5 with `applyConstraints` off.
 */
export function readRuleChecks(response: SearchResponse): RuleCheck[] | null {
    const value = findTraceDetail(response, 'checks');
    return Array.isArray(value) ? value.filter(isRuleCheck) : null;
}

/** The taxonomy notations the query asked for, e.g. `["chargers"]` for "power adapter for my laptop". */
export function readWantedConcepts(response: SearchResponse): string[] {
    const value = findTraceDetail(response, 'wantedConcepts');
    return Array.isArray(value) ? value.filter((notation) => typeof notation === 'string') : [];
}

const operatorPrefixes: Record<string, string> = {
    equals: '',
    greaterOrEqual: '≥ ',
    lessOrEqual: '≤ ',
    in: 'one of ',
};

/** How a check's operator reads before the device's value: "needs ≥ 65W", "needs USB-C". */
export function operatorPrefix(operator: string): string {
    return operatorPrefixes[operator] ?? `${operator} `;
}

function isRuleCheck(value: unknown): value is RuleCheck {
    if (typeof value !== 'object' || value === null) {
        return false;
    }

    const check = value as Record<string, unknown>;
    return (
        typeof check.candidateId === 'string' &&
        typeof check.rule === 'string' &&
        typeof check.definition === 'string' &&
        typeof check.accessorySpec === 'string' &&
        isStringOrNull(check.accessoryValue) &&
        typeof check.operator === 'string' &&
        typeof check.deviceSpec === 'string' &&
        isStringOrNull(check.deviceValue) &&
        (check.result === 'Pass' || check.result === 'Fail' || check.result === 'Unknown')
    );
}

function isStringOrNull(value: unknown): value is string | null {
    return value === null || typeof value === 'string';
}
