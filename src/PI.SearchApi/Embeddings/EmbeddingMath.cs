namespace PI.SearchApi.Embeddings;

/// <summary>
/// The two maths steps that turn a transformer's per-token output into one sentence embedding
/// (ADR-0009). They're pure functions so they can be unit-tested on hand-made numbers.
/// </summary>
public static class EmbeddingMath
{
    /// <summary>
    /// Mean pooling: average the token vectors, counting only real tokens (attention mask = 1), not padding.
    /// </summary>
    /// <param name="tokenEmbeddings">One sequence's hidden states, flattened: tokens × dimensions.</param>
    /// <param name="attentionMask">1 for a real token, 0 for padding; one entry per token.</param>
    /// <param name="dimensions">The embedding size, e.g. 768.</param>
    /// <remarks>
    /// Worked example with 2 dimensions: tokens [1, 2], [3, 4] and a padding token [9, 9] with mask
    /// [1, 1, 0] pool to [(1+3)/2, (2+4)/2] = [2, 3]. Without the mask, padding would drag the average
    /// towards [9, 9], and the same sentence would embed differently depending on its batch-mates.
    /// </remarks>
    public static float[] MeanPool(ReadOnlySpan<float> tokenEmbeddings, ReadOnlySpan<long> attentionMask, int dimensions)
    {
        var pooled = new float[dimensions];
        var realTokens = 0;

        for (var token = 0; token < attentionMask.Length; token++)
        {
            if (attentionMask[token] == 0)
            {
                continue;
            }

            realTokens++;
            var vector = tokenEmbeddings.Slice(token * dimensions, dimensions);

            for (var d = 0; d < dimensions; d++)
            {
                pooled[d] += vector[d];
            }
        }

        // Guard against an all-padding sequence; the same clamp sentence-transformers uses.
        var divisor = Math.Max(realTokens, 1e-9f);

        for (var d = 0; d < dimensions; d++)
        {
            pooled[d] /= divisor;
        }

        return pooled;
    }

    /// <summary>
    /// L2 normalisation: scale the vector to length 1, in place. Once every vector has length 1,
    /// cosine similarity is just the dot product, and only direction — meaning — is compared.
    /// </summary>
    /// <remarks>Worked example: [3, 4] has length √(9 + 16) = 5, so it becomes [0.6, 0.8].</remarks>
    public static float[] L2Normalise(float[] vector)
    {
        var sumOfSquares = 0.0;

        foreach (var value in vector)
        {
            sumOfSquares += value * value;
        }

        var length = (float)Math.Max(Math.Sqrt(sumOfSquares), 1e-12);

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= length;
        }

        return vector;
    }
}
