import type { CompatibilityResult, CompatibilityStatus, SearchResponse } from '@/api/client';

// Reading a trace step's `details`. The contract types `details` as an open dictionary (ADR-0003): each
// stage writes its own keys. These readers check every field at runtime, so a trace that changes shape
// shows up as missing data ("—" or an empty table) rather than a crash. Which reader runs for a step is
// decided by traceStepKind.ts.

type Details = Record<string, unknown>;

// --- Stage 5: rule checks -----------------------------------------------------------------------------

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
    return asStrings(findTraceDetail(response, 'wantedConcepts'));
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

const operatorSymbols: Record<string, string> = {
    equals: '=',
    greaterOrEqual: '≥',
    lessOrEqual: '≤',
    in: '∈',
};

/** The operator as a symbol between two values: "45W ≥ 65W". */
export function operatorSymbol(operator: string): string {
    return operatorSymbols[operator] ?? operator;
}

export interface DeviceSummary {
    id: string;
    name: string;
}

export interface ConstrainDetails {
    applyConstraints: boolean;
    targetDevice: DeviceSummary | null;
    /** How the device was found, e.g. "context.targetProductId" or "device name found in the query". */
    targetDeviceMethod: string | null;
    /** rules.rq, the SPARQL that finds every rule and its checks in the TTL. */
    rulesSparql: string | null;
    rulesApplied: string[];
    checks: RuleCheck[];
    flaggedCount: number;
}

export function readConstrainDetails(details: Details): ConstrainDetails {
    return {
        applyConstraints: details.applyConstraints === true,
        targetDevice: readDevice(details.targetDevice),
        targetDeviceMethod: asString(details.targetDeviceMethod),
        rulesSparql: asString(details.rulesSparql),
        rulesApplied: asStrings(details.rulesApplied),
        checks: Array.isArray(details.checks) ? details.checks.filter(isRuleCheck) : [],
        flaggedCount: Array.isArray(details.flagged) ? details.flagged.length : 0,
    };
}

// --- Stage 5: understand, expand, classify ------------------------------------------------------------

export interface LabelMatch {
    /** The concept's notation, e.g. "chargers" or "usb-c". */
    concept: string;
    /** "taxonomy" for a category, or a value vocabulary such as "connectors". */
    scheme: string;
    label: string;
    language: string;
    /** SKOS label kind: Preferred, Alternative or Hidden. */
    kind: string;
}

export interface PhraseMatch {
    phrase: string;
    labels: LabelMatch[];
}

export interface UnderstandDetails {
    targetDevice: DeviceSummary | null;
    targetDeviceMethod: string | null;
    /** The device name found in the query text, if the device came from there. */
    deviceMention: string | null;
    queryWithoutDevice: string | null;
    tokens: { original: string; folded: string }[];
    matches: PhraseMatch[];
    wantedConcepts: string[];
    contextConcepts: string[];
    remainingText: string | null;
}

export function readUnderstandDetails(details: Details): UnderstandDetails {
    return {
        targetDevice: readDevice(details.targetDevice),
        targetDeviceMethod: asString(details.targetDeviceMethod),
        deviceMention: asString(details.deviceMention),
        queryWithoutDevice: asString(details.queryWithoutDevice),
        tokens: asRecords(details.tokens).map((token) => ({
            original: asString(token.original) ?? '',
            folded: asString(token.folded) ?? '',
        })),
        matches: asRecords(details.matches).map((match) => ({
            phrase: asString(match.phrase) ?? '',
            labels: asRecords(match.labels).map((label) => ({
                concept: asString(label.concept) ?? '',
                scheme: asString(label.scheme) ?? '',
                label: asString(label.label) ?? '',
                language: asString(label.language) ?? '',
                kind: asString(label.kind) ?? '',
            })),
        })),
        wantedConcepts: asStrings(details.wantedConcepts),
        contextConcepts: asStrings(details.contextConcepts),
        remainingText: asString(details.remainingText),
    };
}

export interface ExpandedPhrase {
    phrase: string;
    concepts: string[];
    /** The phrase, then preferred and alternative labels of the concept and its narrower concepts, capped at 10. */
    terms: string[];
}

export interface ExpansionDetails {
    expandSynonyms: boolean;
    phrases: ExpandedPhrase[];
    /** One OR group per wanted phrase, AND-ed with the remaining text in the tsquery. */
    keywordOrGroups: string[][];
    keywordRemainingText: string | null;
    /** The text vector search embeds: the query with the wanted concepts' names appended. */
    embeddingText: string | null;
}

export function readExpansionDetails(details: Details): ExpansionDetails {
    return {
        expandSynonyms: details.expandSynonyms === true,
        phrases: asRecords(details.phrases).map((phrase) => ({
            phrase: asString(phrase.phrase) ?? '',
            concepts: asStrings(phrase.concepts),
            terms: asStrings(phrase.terms),
        })),
        keywordOrGroups: Array.isArray(details.keywordOrGroups)
            ? details.keywordOrGroups.filter(Array.isArray).map(asStrings)
            : [],
        keywordRemainingText: asString(details.keywordRemainingText),
        embeddingText: asString(details.embeddingText),
    };
}

export interface Classification {
    id: string;
    categories: string[];
    conceptMatch: string | null;
    matchedConcept: string | null;
    /** For an in-concept item: its category up to the wanted concept, e.g. laptop-chargers › chargers. */
    broaderChain: string[];
    /** Where each category sits in the taxonomy, up to its top concept. */
    categoryChains: Record<string, string[]>;
}

export interface ClassificationDetails {
    wantedConcepts: string[];
    contextConcepts: string[];
    classifications: Classification[];
}

export function readClassificationDetails(details: Details): ClassificationDetails {
    return {
        wantedConcepts: asStrings(details.wantedConcepts),
        contextConcepts: asStrings(details.contextConcepts),
        classifications: asRecords(details.classifications).map((item) => ({
            id: asString(item.id) ?? '',
            categories: asStrings(item.categories),
            conceptMatch: asString(item.conceptMatch),
            matchedConcept: asString(item.matchedConcept),
            broaderChain: asStrings(item.broaderChain),
            categoryChains: isRecord(item.categoryChains)
                ? Object.fromEntries(
                      Object.entries(item.categoryChains).map(([category, chain]) => [
                          category,
                          asStrings(chain),
                      ]),
                  )
                : {},
        })),
    };
}

// --- Stages 1–4 ---------------------------------------------------------------------------------------

export interface StructuredDetails {
    rowCount: number | null;
    totalCount: number | null;
    countSql: string | null;
}

export function readStructuredDetails(details: Details): StructuredDetails {
    return {
        rowCount: asNumber(details.rowCount),
        totalCount: asNumber(details.totalCount),
        countSql: asString(details.countSql),
    };
}

export interface KeywordMatch {
    id: string;
    rank: number | null;
    score: number | null;
    /** The query lexemes found in this product's text. */
    matchedTerms: string[];
}

export interface KeywordDetails {
    /** The tsquery Postgres built: lexemes joined by & (AND), | (OR) and <-> (followed by). */
    tsquery: string | null;
    /** The SQL expression that built it, e.g. `websearch_to_tsquery('english', @query)`. */
    tsqueryExpression: string | null;
    isExpanded: boolean;
    ranking: string | null;
    matches: KeywordMatch[];
}

export function readKeywordDetails(details: Details): KeywordDetails {
    return {
        tsquery: asString(details.tsquery),
        tsqueryExpression: asString(details.tsqueryExpression),
        isExpanded: details.expanded === true,
        ranking: asString(details.ranking),
        matches: asRecords(details.matches).map((match) => ({
            id: asString(match.id) ?? '',
            rank: asNumber(match.rank),
            score: asNumber(match.score),
            matchedTerms: asStrings(match.matchedTerms),
        })),
    };
}

export interface EmbeddingDetails {
    provider: string | null;
    model: string | null;
    /** The task prefix the model was trained with, e.g. "search_query: ". */
    prefix: string | null;
    embeddedText: string | null;
    tokenCount: number | null;
    isTruncated: boolean;
    dimensions: number | null;
    firstDimensions: number[];
}

export function readEmbeddingDetails(details: Details): EmbeddingDetails {
    return {
        provider: asString(details.provider),
        model: asString(details.model),
        prefix: asString(details.prefix),
        embeddedText: asString(details.embeddedText),
        tokenCount: asNumber(details.tokenCount),
        isTruncated: details.truncated === true,
        dimensions: asNumber(details.dimensions),
        firstDimensions: Array.isArray(details.firstDimensions)
            ? details.firstDimensions.filter((value) => typeof value === 'number')
            : [],
    };
}

export interface VectorDistance {
    id: string;
    rank: number | null;
    distance: number | null;
    similarity: number | null;
}

export interface DistanceDetails {
    metric: string | null;
    activeModel: string | null;
    efSearch: number | null;
    distances: VectorDistance[];
}

export function readDistanceDetails(details: Details): DistanceDetails {
    return {
        metric: asString(details.metric),
        activeModel: asString(details.activeModel),
        efSearch: asNumber(details.efSearch),
        distances: asRecords(details.distances).map((item) => ({
            id: asString(item.id) ?? '',
            rank: asNumber(item.rank),
            distance: asNumber(item.distance),
            similarity: asNumber(item.similarity),
        })),
    };
}

export interface RrfRow {
    id: string;
    /** The sum as the API wrote it, e.g. "1/(60+3) + 1/(60+1)". */
    expression: string;
    total: string;
}

export interface RrfDetails {
    formula: string | null;
    k: number | null;
    weights: Record<string, number>;
    listSizes: Record<string, number>;
    /** How many items both retrievers returned. */
    overlap: number | null;
    /** In fused order, best first. */
    rows: RrfRow[];
}

export function readRrfDetails(details: Details): RrfDetails {
    return {
        formula: asString(details.formula),
        k: asNumber(details.k),
        weights: asNumberRecord(details.weights),
        listSizes: asNumberRecord(details.listSizes),
        overlap: asNumber(details.overlap),
        rows: asStrings(details.formulas).flatMap((formula) => parseRrfFormula(formula) ?? []),
    };
}

/**
 * Splits one of the API's RRF formula strings into its parts. The maths stays the API's own text
 * (ADR-0014: formulas are rendered as text from the API, never recomputed in the UI).
 *
 * "PROD-0012: 1/(60+3) + 1/(60+1) = 0.03227" → { id: "PROD-0012", expression: "1/(60+3) + 1/(60+1)", total: "0.03227" }
 */
export function parseRrfFormula(formula: string): RrfRow | null {
    const [, id, expression, total] = /^(\S+): (.+) = (\S+)$/.exec(formula) ?? [];
    return id !== undefined && expression !== undefined && total !== undefined
        ? { id, expression, total }
        : null;
}

// --- Stages 6–7: evidence, prompts, generation and validation ------------------------------------------

/** Why a product is in the evidence set (ADR-0016). */
export type EvidenceRole = 'TargetDevice' | 'Compatible' | 'Incompatible' | 'Unknown' | 'NotChecked';

const evidenceRoles: readonly EvidenceRole[] = [
    'TargetDevice',
    'Compatible',
    'Incompatible',
    'Unknown',
    'NotChecked',
];
const compatibilityStatuses: readonly CompatibilityStatus[] = [
    'NotEvaluated',
    'Compatible',
    'Incompatible',
    'Unknown',
];

export interface EvidenceItem {
    id: string;
    name: string;
    role: EvidenceRole;
    /** The product's place in Stage 5's order; `null` for the target device. */
    rank: number | null;
    compatibility: CompatibilityResult;
    conceptMatch: string | null;
    whyIncluded: string;
    description: string | null;
}

/** A matched concept, in the ontology's own words: preferred label, alternative labels and definition. */
export interface EvidenceConcept {
    notation: string;
    prefLabel: string;
    altLabels: string[];
    definition: string | null;
}

export interface EvidenceRule {
    name: string;
    definitions: string[];
    specTerms: string[];
}

export interface EvidenceDetails {
    items: EvidenceItem[];
    concepts: EvidenceConcept[];
    rules: EvidenceRule[];
    /** How many products of each kind the model may be given, e.g. `{ compatible: 5, incompatible: 3 }`. */
    limits: Record<string, number>;
}

export function readEvidenceDetails(details: Details): EvidenceDetails {
    return {
        items: asRecords(details.evidence).flatMap((item): EvidenceItem[] => {
            const id = asString(item.id);
            const role = evidenceRoles.find((candidate) => candidate === item.role);
            const status = compatibilityStatuses.find((candidate) => candidate === item.compatibility);

            return id === null || role === undefined || status === undefined
                ? []
                : [
                      {
                          id,
                          name: asString(item.name) ?? id,
                          role,
                          rank: asNumber(item.rank),
                          compatibility: { status, reasons: asStrings(item.reasons) },
                          conceptMatch: asString(item.conceptMatch),
                          whyIncluded: asString(item.whyIncluded) ?? '',
                          description: asString(item.description),
                      },
                  ];
        }),
        concepts: asRecords(details.concepts).flatMap((concept): EvidenceConcept[] => {
            const notation = asString(concept.notation);
            return notation === null
                ? []
                : [
                      {
                          notation,
                          prefLabel: asString(concept.prefLabel) ?? notation,
                          altLabels: asStrings(concept.altLabels),
                          definition: asString(concept.definition),
                      },
                  ];
        }),
        rules: asRecords(details.rules).flatMap((rule): EvidenceRule[] => {
            const name = asString(rule.name);
            return name === null
                ? []
                : [{ name, definitions: asStrings(rule.definitions), specTerms: asStrings(rule.specTerms) }];
        }),
        limits: asNumberRecord(details.limits),
    };
}

/** The evidence set from a Stage 6–7 results response (its evidence trace step), or `null` for other stages. */
export function readEvidence(response: SearchResponse): EvidenceDetails | null {
    const step = response.debugTrace.steps.find(
        ({ details }) =>
            details != null && Object.hasOwn(details, 'evidence') && Object.hasOwn(details, 'limits'),
    );

    return step?.details == null ? null : readEvidenceDetails(step.details);
}

/** Which model answered and how it was asked (ADR-0015). The API never puts the key in the trace. */
export interface LlmInfo {
    provider: string;
    model: string;
    endpointHost: string | null;
    settings: Record<string, string | number | boolean | null>;
}

export interface PromptDetails {
    /** `answer` or `explanation`. */
    section: string | null;
    /** Stage 7 only: whether the pedagogy prompt (true) or the baseline (false) was used. */
    applyPedagogy: boolean | null;
    audience: string | null;
    promptFiles: string[];
    systemPrompt: string;
    userPrompt: string;
    /** The active audience's section of pedagogy-audiences.md, as it appears in the system prompt. */
    audienceGuidance: string | null;
    /** Each concept's words for the audience, by notation. */
    wordsOffered: Record<string, string[]>;
    llm: LlmInfo | null;
}

export function readPromptDetails(details: Details): PromptDetails {
    return {
        section: asString(details.section),
        applyPedagogy: asBoolean(details.applyPedagogy),
        audience: asString(details.audience),
        promptFiles: asStrings(details.promptFiles),
        systemPrompt: asString(details.systemPrompt) ?? '',
        userPrompt: asString(details.userPrompt) ?? '',
        audienceGuidance: asString(details.audienceGuidance),
        wordsOffered: isRecord(details.wordsOffered)
            ? Object.fromEntries(
                  Object.entries(details.wordsOffered).map(([notation, words]) => [
                      notation,
                      asStrings(words),
                  ]),
              )
            : {},
        llm: readLlm(details.llm),
    };
}

export interface SectionTiming {
    timeToFirstTokenMs: number | null;
    totalMs: number | null;
    /** The explanation only: when it started, measured from the start of the request. */
    startedAtMs: number | null;
}

export interface GenerationDetails {
    section: string | null;
    llm: LlmInfo | null;
    rawOutput: string;
    timeToFirstTokenMs: number | null;
    totalMs: number | null;
    finishReason: string | null;
    inputTokens: number | null;
    outputTokens: number | null;
    /** Stage 7's per-section timings (ADR-0017); `null` on Stage 6. */
    answerTiming: SectionTiming | null;
    explanationTiming: SectionTiming | null;
}

export function readGenerationDetails(details: Details): GenerationDetails {
    const timings = isRecord(details.sectionTimings) ? details.sectionTimings : null;

    return {
        section: asString(details.section),
        llm: readLlm(details.llm),
        rawOutput: asString(details.rawOutput) ?? '',
        timeToFirstTokenMs: asNumber(details.timeToFirstTokenMs),
        totalMs: asNumber(details.totalMs),
        finishReason: asString(details.finishReason),
        inputTokens: asNumber(details.inputTokens),
        outputTokens: asNumber(details.outputTokens),
        answerTiming: readTiming(timings?.answer),
        explanationTiming: readTiming(timings?.explanation),
    };
}

/** One check run on finished LLM text. A heuristic guesses from wording rather than proving something. */
export interface ValidationCheck {
    name: string;
    passed: boolean;
    detail: string;
    isHeuristic: boolean;
}

/** Stage 7's parsed headings: the products Decision and Near miss cite, and the concepts' bold terms. */
export interface ExplanationStructureSummary {
    decision: string | null;
    concepts: string[];
    nearMiss: string | null;
    ruleOfThumb: string | null;
    nextStep: string | null;
}

export interface ValidationDetails {
    section: string | null;
    applyPedagogy: boolean | null;
    citations: string[];
    invalidCitations: string[];
    insufficientEvidence: boolean | null;
    checks: ValidationCheck[];
    warnings: string[];
    structure: ExplanationStructureSummary | null;
}

export function readValidationDetails(details: Details): ValidationDetails {
    const structure = isRecord(details.structure) ? details.structure : null;

    return {
        section: asString(details.section),
        applyPedagogy: asBoolean(details.applyPedagogy),
        citations: asStrings(details.citations),
        invalidCitations: asStrings(details.invalidCitations),
        insufficientEvidence: asBoolean(details.insufficientEvidence),
        checks: asRecords(details.checks).flatMap((check): ValidationCheck[] => {
            const name = asString(check.name);
            return name === null || typeof check.passed !== 'boolean'
                ? []
                : [
                      {
                          name,
                          passed: check.passed,
                          detail: asString(check.detail) ?? '',
                          isHeuristic: check.isHeuristic === true,
                      },
                  ];
        }),
        warnings: asStrings(details.warnings),
        structure:
            structure === null
                ? null
                : {
                      decision: isRecord(structure.decision) ? asString(structure.decision.productId) : null,
                      concepts: asStrings(structure.concepts),
                      nearMiss: isRecord(structure.nearMiss) ? asString(structure.nearMiss.productId) : null,
                      ruleOfThumb: asString(structure.ruleOfThumb),
                      nextStep: asString(structure.nextStep),
                  },
    };
}

function readLlm(value: unknown): LlmInfo | null {
    if (!isRecord(value)) {
        return null;
    }

    const provider = asString(value.provider);
    const model = asString(value.model);
    if (provider === null || model === null) {
        return null;
    }

    const settings = isRecord(value.settings)
        ? (Object.fromEntries(
              Object.entries(value.settings).filter(
                  ([, setting]) =>
                      setting === null || ['string', 'number', 'boolean'].includes(typeof setting),
              ),
          ) as Record<string, string | number | boolean | null>)
        : {};

    return { provider, model, endpointHost: asString(value.endpointHost), settings };
}

function readTiming(value: unknown): SectionTiming | null {
    return isRecord(value)
        ? {
              timeToFirstTokenMs: asNumber(value.timeToFirstTokenMs),
              totalMs: asNumber(value.totalMs),
              startedAtMs: asNumber(value.startedAtMs),
          }
        : null;
}

// --- Shared -------------------------------------------------------------------------------------------

/** Product names by ID from the response, so trace tables can name products the trace only lists by ID. */
export function productNames(response: SearchResponse): Map<string, string> {
    return new Map(response.results.map((product) => [product.id, product.name]));
}

export function isRecord(value: unknown): value is Details {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readDevice(value: unknown): DeviceSummary | null {
    if (!isRecord(value)) {
        return null;
    }

    const id = asString(value.id);
    return id === null ? null : { id, name: asString(value.name) ?? id };
}

function asString(value: unknown): string | null {
    return typeof value === 'string' ? value : null;
}

function asNumber(value: unknown): number | null {
    return typeof value === 'number' ? value : null;
}

function asBoolean(value: unknown): boolean | null {
    return typeof value === 'boolean' ? value : null;
}

function asStrings(value: unknown): string[] {
    return Array.isArray(value) ? value.filter((item) => typeof item === 'string') : [];
}

function asRecords(value: unknown): Details[] {
    return Array.isArray(value) ? value.filter(isRecord) : [];
}

function asNumberRecord(value: unknown): Record<string, number> {
    if (!isRecord(value)) {
        return {};
    }

    return Object.fromEntries(
        Object.entries(value).filter((entry): entry is [string, number] => typeof entry[1] === 'number'),
    );
}

function isRuleCheck(value: unknown): value is RuleCheck {
    if (!isRecord(value)) {
        return false;
    }

    return (
        typeof value.candidateId === 'string' &&
        typeof value.rule === 'string' &&
        typeof value.definition === 'string' &&
        typeof value.accessorySpec === 'string' &&
        isStringOrNull(value.accessoryValue) &&
        typeof value.operator === 'string' &&
        typeof value.deviceSpec === 'string' &&
        isStringOrNull(value.deviceValue) &&
        (value.result === 'Pass' || value.result === 'Fail' || value.result === 'Unknown')
    );
}

function isStringOrNull(value: unknown): value is string | null {
    return value === null || typeof value === 'string';
}
