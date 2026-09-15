using PI.SearchApi.Endpoints.Search;
using Xunit;

namespace PI.SearchApi.Tests.Endpoints.Search;

public sealed class ServerSentEventWriterTests
{
    [Fact]
    public void Format_SingleLineJson_WritesEventDataAndBlankLine()
    {
        var text = ServerSentEventWriter.Format("delta", """{"section":"answer","text":"Hi"}""");

        Assert.Equal("event: delta\ndata: {\"section\":\"answer\",\"text\":\"Hi\"}\n\n", text);
    }

    [Fact]
    public void Format_MultiLinePayload_WritesOneDataLinePerLine()
    {
        // The SSE format ends an event at a blank line, so a raw newline inside data would split the event.
        var text = ServerSentEventWriter.Format("final", "{\r\n\"a\": 1\n}");

        Assert.Equal("event: final\ndata: {\ndata: \"a\": 1\ndata: }\n\n", text);
    }
}
