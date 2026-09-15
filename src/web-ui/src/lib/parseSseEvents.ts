// A small, readable Server-Sent Events parser (ADR-0014 § State and data flow). The browser's EventSource would
// parse the stream for us, but it can only GET, and /answer needs the same POST body as the search.
//
// The format is plain text: lines of `field: value`, and a blank line ends each event. Chunks from the network can
// split anywhere, even inside a line or between \r and \n, so the parser keeps what it hasn't finished and returns
// only complete events.

export interface ServerSentEvent {
    /** The `event:` field; "message" when the event doesn't name itself. */
    event: string;
    /** Every `data:` line of the event, joined with newlines. */
    data: string;
}

export interface SseParser {
    /** Adds a chunk of decoded text and returns the events it completed, in order. */
    push: (chunk: string) => ServerSentEvent[];
}

export function createSseParser(): SseParser {
    let buffer = '';
    let eventName = '';
    let dataLines: string[] = [];

    function processLine(line: string, events: ServerSentEvent[]) {
        if (line === '') {
            // A blank line dispatches the event. One with no data carries nothing, so it's dropped (as EventSource does).
            if (dataLines.length > 0) {
                events.push({ event: eventName === '' ? 'message' : eventName, data: dataLines.join('\n') });
            }
            eventName = '';
            dataLines = [];
            return;
        }

        // A line starting with a colon is a comment, often sent as a keep-alive.
        if (line.startsWith(':')) {
            return;
        }

        const colon = line.indexOf(':');
        const field = colon === -1 ? line : line.slice(0, colon);
        const rawValue = colon === -1 ? '' : line.slice(colon + 1);
        // One space after the colon is part of the format, not the value.
        const value = rawValue.startsWith(' ') ? rawValue.slice(1) : rawValue;

        if (field === 'event') {
            eventName = value;
        } else if (field === 'data') {
            dataLines.push(value);
        }
    }

    return {
        push(chunk) {
            buffer += chunk;
            const events: ServerSentEvent[] = [];

            for (;;) {
                const end = buffer.search(/[\r\n]/);
                if (end === -1) {
                    break;
                }

                // A \r at the very end may be the first half of \r\n: wait for the next chunk to know.
                if (buffer[end] === '\r' && end === buffer.length - 1) {
                    break;
                }

                const separatorLength = buffer[end] === '\r' && buffer[end + 1] === '\n' ? 2 : 1;
                processLine(buffer.slice(0, end), events);
                buffer = buffer.slice(end + separatorLength);
            }

            return events;
        },
    };
}
