import { describe, expect, it } from 'vitest';
import { createSseParser } from './parseSseEvents';

describe('createSseParser', () => {
    it('returns each complete event with its name and data', () => {
        const parser = createSseParser();

        const events = parser.push(
            'event: meta\ndata: {"stage":"rag"}\n\nevent: delta\ndata: {"text":"Hi"}\n\n',
        );

        expect(events).toEqual([
            { event: 'meta', data: '{"stage":"rag"}' },
            { event: 'delta', data: '{"text":"Hi"}' },
        ]);
    });

    it('keeps an event split across chunks until its blank line arrives', () => {
        const parser = createSseParser();

        expect(parser.push('event: del')).toEqual([]);
        expect(parser.push('ta\ndata: {"te')).toEqual([]);
        expect(parser.push('xt":"Hi"}\n')).toEqual([]);
        expect(parser.push('\n')).toEqual([{ event: 'delta', data: '{"text":"Hi"}' }]);
    });

    it('accepts \\r\\n line endings, even when a chunk ends between \\r and \\n', () => {
        const parser = createSseParser();

        expect(parser.push('event: done\r\ndata: {}\r')).toEqual([]);
        expect(parser.push('\n\r\n')).toEqual([{ event: 'done', data: '{}' }]);
    });

    it('joins several data lines with newlines, and ignores comments', () => {
        const parser = createSseParser();

        const events = parser.push(': keep-alive\ndata: line one\ndata: line two\n\n');

        expect(events).toEqual([{ event: 'message', data: 'line one\nline two' }]);
    });

    it('drops an event with no data', () => {
        expect(createSseParser().push('event: ping\n\n')).toEqual([]);
    });
});
