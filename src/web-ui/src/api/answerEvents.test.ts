import { describe, expect, it } from 'vitest';
import { toAnswerEvent } from './answerEvents';

describe('toAnswerEvent', () => {
    it('reads meta, delta, final and done events', () => {
        expect(
            toAnswerEvent(
                'meta',
                '{"stage":"rag","provider":"ollama","model":"qwen3.6:35b","evidence":["PROD-0001"]}',
            ),
        ).toEqual({
            name: 'meta',
            data: { stage: 'rag', provider: 'ollama', model: 'qwen3.6:35b', evidence: ['PROD-0001'] },
        });
        expect(toAnswerEvent('delta', '{"section":"explanation","text":"## Decision"}')?.name).toBe('delta');
        expect(
            toAnswerEvent(
                'final',
                '{"section":"answer","markdown":"x","citations":[],"invalidCitations":[],"insufficientEvidence":false,"warnings":[],"structure":null}',
            )?.name,
        ).toBe('final');
        expect(toAnswerEvent('done', '{"timeToFirstTokenMs":56,"totalMs":2300,"trace":[]}')?.name).toBe(
            'done',
        );
    });

    it('reads an error event as ProblemDetails', () => {
        expect(toAnswerEvent('error', '{"status":503,"title":"LLM unavailable"}')).toEqual({
            name: 'error',
            data: { status: 503, title: 'LLM unavailable' },
        });
    });

    it('skips unknown events, malformed JSON and events missing fields', () => {
        expect(toAnswerEvent('heartbeat', '{}')).toBeNull();
        expect(toAnswerEvent('delta', '{not json')).toBeNull();
        expect(toAnswerEvent('delta', '{"section":"summary","text":"x"}')).toBeNull();
        expect(toAnswerEvent('final', '{"section":"answer","markdown":"x"}')).toBeNull();
    });
});
