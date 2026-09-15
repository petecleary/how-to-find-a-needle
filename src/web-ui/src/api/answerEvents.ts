import type { AnswerFinal, ProblemDetails, TraceStep } from './client';

// The answer stream's events (ADR-0016, ADR-0017), typed by hand. OpenAPI describes the JSON form of /answer
// (AnswerResponse) but can't describe the events of a text/event-stream, so these mirror Contracts/Answer*.cs.
// `final` reuses the generated AnswerFinal. Every event is checked at runtime before the UI trusts it, like the
// trace readers in lib/traceDetails.ts: a malformed or unknown event is skipped, not a crash.

export const answerSections = ['answer', 'explanation'] as const;
export type AnswerSection = (typeof answerSections)[number];

/** The first event: which model is answering, and which products it was given. */
export interface AnswerMeta {
    stage: string;
    provider: string;
    model: string;
    /** Product IDs in the evidence set, target device first. */
    evidence: string[];
}

/** One chunk of streamed markdown. It hasn't been validated yet: the section's `final` event does that. */
export interface AnswerDelta {
    section: AnswerSection;
    text: string;
}

/** The last event of a successful stream: timings and the answer's own trace steps. */
export interface AnswerDone {
    timeToFirstTokenMs: number | null;
    totalMs: number;
    trace: TraceStep[];
}

export type AnswerEvent =
    | { name: 'meta'; data: AnswerMeta }
    | { name: 'delta'; data: AnswerDelta }
    | { name: 'final'; data: AnswerFinal }
    | { name: 'done'; data: AnswerDone }
    | { name: 'error'; data: ProblemDetails };

export function isAnswerSection(value: unknown): value is AnswerSection {
    return answerSections.some((section) => section === value);
}

/** Turns an SSE event's name and JSON data into a typed answer event, or `null` if it isn't one. */
export function toAnswerEvent(name: string, json: string): AnswerEvent | null {
    let data: unknown;
    try {
        data = JSON.parse(json);
    } catch {
        return null;
    }

    if (!isObject(data)) {
        return null;
    }

    switch (name) {
        case 'meta':
            return typeof data.stage === 'string' &&
                typeof data.provider === 'string' &&
                typeof data.model === 'string' &&
                isStringArray(data.evidence)
                ? {
                      name,
                      data: {
                          stage: data.stage,
                          provider: data.provider,
                          model: data.model,
                          evidence: data.evidence,
                      },
                  }
                : null;
        case 'delta':
            return isAnswerSection(data.section) && typeof data.text === 'string'
                ? { name, data: { section: data.section, text: data.text } }
                : null;
        case 'final':
            return isAnswerSection(data.section) &&
                typeof data.markdown === 'string' &&
                isStringArray(data.citations) &&
                isStringArray(data.invalidCitations) &&
                typeof data.insufficientEvidence === 'boolean' &&
                isStringArray(data.warnings)
                ? { name, data: data as unknown as AnswerFinal }
                : null;
        case 'done':
            return typeof data.totalMs === 'number' &&
                (data.timeToFirstTokenMs === null || typeof data.timeToFirstTokenMs === 'number') &&
                Array.isArray(data.trace)
                ? { name, data: data as unknown as AnswerDone }
                : null;
        case 'error':
            return { name, data: data as ProblemDetails };
        default:
            return null;
    }
}

function isObject(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringArray(value: unknown): value is string[] {
    return Array.isArray(value) && value.every((item) => typeof item === 'string');
}
