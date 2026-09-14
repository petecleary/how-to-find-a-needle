using PI.SearchApi.Pipeline.Fusion;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Fusion;

public sealed class ReciprocalRankFusionTests
{
    private static readonly ReciprocalRankFusion Rrf = new();

    private static RankedList Keyword(params string[] ids) => new("keyword", 1.0, ids);

    private static RankedList Vector(params string[] ids) => new("vector", 1.0, ids);

    [Fact]
    public void Fuse_ItemInBothLists_RanksAboveItemInOneList()
    {
        // A: 1/(60+1) + 1/(60+1) = 0.03279; B: 1/(60+2) = 0.01613; C: 1/(60+2) = 0.01613 → A first.
        var fused = Rrf.Fuse([Keyword("A", "B"), Vector("A", "C")], k: 60);

        Assert.Equal("A", fused[0].Id);
        Assert.Equal(0.03279, fused[0].Score, 5);
        Assert.Equal(0.01613, fused[1].Score, 5);
    }

    [Fact]
    public void Fuse_TalkExample_ProducesTheWorkedFormula()
    {
        // The talk's slide: 2nd in keyword, 1st in vector → 1/(60+2) + 1/(60+1) = 0.01613 + 0.01639 = 0.03252.
        var fused = Rrf.Fuse([Keyword("PROD-0011", "PROD-0012"), Vector("PROD-0012")], k: 60);

        var item = Assert.Single(fused, f => f.Id == "PROD-0012");
        Assert.Equal("PROD-0012: 1/(60+2) + 1/(60+1) = 0.03252", item.Formula);
        Assert.Equal(1, item.FusedRank);
    }

    [Fact]
    public void Fuse_ItemMissingFromAList_ContributesZeroAndSaysSo()
    {
        // PROD-0031 is 7th in keyword and absent from vector: 1/(60+7) = 0.01493.
        var fused = Rrf.Fuse([Keyword("P1", "P2", "P3", "P4", "P5", "P6", "PROD-0031"), Vector("P1")], k: 60);

        var item = Assert.Single(fused, f => f.Id == "PROD-0031");
        Assert.Equal("PROD-0031: 1/(60+7) + — = 0.01493 (not in vector list)", item.Formula);
        Assert.Equal(7, item.Ranks["keyword"]);
        Assert.Null(item.Ranks["vector"]);
    }

    [Fact]
    public void Fuse_EmptyList_LeavesTheOtherListsOrderUnchanged()
    {
        var fused = Rrf.Fuse([Keyword("C", "A", "B"), Vector()], k: 60);

        Assert.Equal(["C", "A", "B"], fused.Select(f => f.Id));
    }

    [Fact]
    public void Fuse_AllListsEmpty_ReturnsNothing()
    {
        Assert.Empty(Rrf.Fuse([Keyword(), Vector()], k: 60));
    }

    [Fact]
    public void Fuse_SingleList_KeepsItsOriginalOrder()
    {
        var fused = Rrf.Fuse([Keyword("Z", "Y", "X", "W")], k: 60);

        Assert.Equal(["Z", "Y", "X", "W"], fused.Select(f => f.Id));
        Assert.Equal([1, 2, 3, 4], fused.Select(f => f.FusedRank));
    }

    [Fact]
    public void Fuse_WeightZero_RemovesThatListsInfluence()
    {
        // Vector disagrees completely, but with weight 0 only keyword counts: A = 1/61, B = 1/62 → A first.
        var fused = Rrf.Fuse([Keyword("A", "B"), new RankedList("vector", 0.0, ["B", "A"])], k: 60);

        Assert.Equal(["A", "B"], fused.Select(f => f.Id));
        Assert.Equal("A: 1/(60+1) + 0/(60+2) = 0.01639", fused[0].Formula);
    }

    [Fact]
    public void Fuse_HigherWeight_LetsThatListWin()
    {
        // Keyword weight 2: A = 2/(60+1) = 0.03279; B = 1/(60+1) = 0.01639 → A first, though vector preferred B.
        var fused = Rrf.Fuse([new RankedList("keyword", 2.0, ["A"]), Vector("B")], k: 60);

        Assert.Equal(["A", "B"], fused.Select(f => f.Id));
    }

    [Fact]
    public void Fuse_EqualScores_BreakTiesByBestRankThenId()
    {
        // B and A both score 1/61 and both have best rank 1 → ID order decides: A, then B.
        var fused = Rrf.Fuse([Keyword("B"), Vector("A")], k: 60);

        Assert.Equal(["A", "B"], fused.Select(f => f.Id));
    }

    [Fact]
    public void Fuse_EqualScores_BetterBestRankWinsBeforeId()
    {
        // With k = 1: Z is 1st in keyword (weight 1) → 1/(1+1) = 0.5.
        //             R is 2nd in vector (weight 1.5) → 1.5/(1+2) = 0.5. An exact tie.
        // ID order alone would put R first; best rank (Z: 1, R: 2) puts Z first.
        var fused = Rrf.Fuse([new RankedList("keyword", 1.0, ["Z"]), new RankedList("vector", 1.5, ["S", "R"])], k: 1);

        var z = fused.ToList().FindIndex(f => f.Id == "Z");
        var r = fused.ToList().FindIndex(f => f.Id == "R");
        Assert.Equal(fused[z].Score, fused[r].Score);
        Assert.True(z < r);
    }

    [Fact]
    public void Fuse_SmallK_TopRankDominates_LargeK_AgreementWins()
    {
        // A: 1st in keyword, 20th in vector. B: 3rd in both.
        // k = 1:  A = 1/2 + 1/21 = 0.5476;   B = 1/4 + 1/4 = 0.5000   → A wins (top rank dominates).
        // k = 60: A = 1/61 + 1/80 = 0.02889; B = 1/63 + 1/63 = 0.03175 → B wins (agreement wins).
        var vectorIds = Enumerable.Range(1, 20).Select(i => i switch { 3 => "B", 20 => "A", _ => $"v{i}" }).ToArray();
        RankedList[] lists = [Keyword("A", "k2", "B"), Vector(vectorIds)];

        var smallK = Rrf.Fuse(lists, k: 1);
        var largeK = Rrf.Fuse(lists, k: 60);

        Assert.True(smallK.ToList().FindIndex(f => f.Id == "A") < smallK.ToList().FindIndex(f => f.Id == "B"));
        Assert.True(largeK.ToList().FindIndex(f => f.Id == "B") < largeK.ToList().FindIndex(f => f.Id == "A"));
    }

    [Fact]
    public void Fuse_DuplicateIdInOneList_UsesItsFirstRank()
    {
        var fused = Rrf.Fuse([Keyword("A", "B", "A")], k: 60);

        Assert.Equal(1, Assert.Single(fused, f => f.Id == "A").Ranks["keyword"]);
        Assert.Equal(2, fused.Count);
    }

    [Fact]
    public void Fuse_InputOrderOfLists_DoesNotChangeTheResult()
    {
        var forwards = Rrf.Fuse([Keyword("A", "B", "C"), Vector("C", "A", "D")], k: 60);
        var backwards = Rrf.Fuse([Vector("C", "A", "D"), Keyword("A", "B", "C")], k: 60);

        Assert.Equal(forwards.Select(f => f.Id), backwards.Select(f => f.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Fuse_KBelowOne_Throws(int k)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rrf.Fuse([Keyword("A")], k));
    }
}
