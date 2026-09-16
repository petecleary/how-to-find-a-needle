using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// One event in an answer stream: its SSE event name and its payload (ADR-0016). The endpoint writes these as
/// Server-Sent Events, or gathers them into one <see cref="AnswerResponse"/> for JSON clients.
/// </summary>
public sealed record AnswerEvent(string Name, object Data)
{
    public const string MetaName = "meta";
    public const string DeltaName = "delta";
    public const string FinalName = "final";
    public const string DoneName = "done";
    public const string ErrorName = "error";

    public static AnswerEvent Meta(AnswerMeta meta) => new(MetaName, meta);

    public static AnswerEvent Delta(string section, string text) => new(DeltaName, new AnswerDelta { Section = section, Text = text });

    public static AnswerEvent Final(AnswerFinal final) => new(FinalName, final);

    public static AnswerEvent Done(AnswerDone done) => new(DoneName, done);
}
