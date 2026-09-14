using PI.SearchApi.Embeddings;
using Xunit;

namespace PI.SearchApi.Tests.Embeddings;

public sealed class EmbeddingMathTests
{
    [Fact]
    public void MeanPool_IgnoresPaddingTokens()
    {
        // Tokens [1, 2] and [3, 4] are real; [9, 9] is padding (mask 0).
        // (1+3)/2 = 2, (2+4)/2 = 3 → [2, 3]. Including padding would give [4.33, 5].
        float[] tokens = [1, 2, 3, 4, 9, 9];
        long[] mask = [1, 1, 0];

        var pooled = EmbeddingMath.MeanPool(tokens, mask, dimensions: 2);

        Assert.Equal([2f, 3f], pooled);
    }

    [Fact]
    public void MeanPool_SingleToken_ReturnsThatToken()
    {
        var pooled = EmbeddingMath.MeanPool([0.5f, -0.25f], [1], dimensions: 2);

        Assert.Equal([0.5f, -0.25f], pooled);
    }

    [Fact]
    public void MeanPool_AllPadding_ReturnsZerosRatherThanNaN()
    {
        var pooled = EmbeddingMath.MeanPool([5f, 5f], [0], dimensions: 2);

        Assert.All(pooled, value => Assert.Equal(0f, value));
    }

    [Fact]
    public void L2Normalise_ThreeFour_BecomesPointSixPointEight()
    {
        // |[3, 4]| = √(9 + 16) = 5 → [3/5, 4/5].
        var normalised = EmbeddingMath.L2Normalise([3f, 4f]);

        Assert.Equal(0.6f, normalised[0], 5);
        Assert.Equal(0.8f, normalised[1], 5);
    }

    [Fact]
    public void L2Normalise_AnyVector_HasLengthOne()
    {
        var normalised = EmbeddingMath.L2Normalise([0.2f, -1.7f, 3.1f, 0.05f]);

        var length = Math.Sqrt(normalised.Sum(v => (double)v * v));
        Assert.Equal(1.0, length, 5);
    }

    [Fact]
    public void L2Normalise_ZeroVector_StaysZero()
    {
        var normalised = EmbeddingMath.L2Normalise([0f, 0f]);

        Assert.All(normalised, value => Assert.Equal(0f, value));
    }
}
