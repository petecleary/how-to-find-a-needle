namespace PI.SearchApi.Pipeline.Fusion;

/// <summary>
/// Combines several ranked lists into one ranking (ADR-0004, ADR-0011). Pure: no I/O, so it can be
/// tested exhaustively, and reused by any stage that fuses retrievers (Stages 4 and 6, and 5 if built).
/// </summary>
public interface IRankFusion
{
    /// <summary>Fuses <paramref name="lists"/> with constant <paramref name="k"/> (≥ 1), best first.</summary>
    IReadOnlyList<FusedItem> Fuse(IReadOnlyList<RankedList> lists, int k);
}
