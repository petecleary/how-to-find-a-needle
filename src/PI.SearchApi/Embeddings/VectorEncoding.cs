using System.Buffers.Binary;

namespace PI.SearchApi.Embeddings;

/// <summary>
/// Encodes a vector as base64 over little-endian float32 bytes: the compact, readable-enough form
/// used in the committed <c>assets/data/embeddings/*.jsonl</c> files (ADR-0009). 768 floats become
/// 3,072 bytes and about 4,100 base64 characters, instead of ~7,000 characters of decimal text.
/// </summary>
public static class VectorEncoding
{
    public static string ToBase64(ReadOnlySpan<float> vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];

        for (var i = 0; i < vector.Length; i++)
        {
            // Little-endian is written explicitly, so the file reads the same on any machine.
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), vector[i]);
        }

        return Convert.ToBase64String(bytes);
    }

    public static float[] FromBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);

        if (bytes.Length % sizeof(float) != 0)
        {
            throw new FormatException($"A float32 vector needs a byte count divisible by 4; got {bytes.Length}.");
        }

        var vector = new float[bytes.Length / sizeof(float)];

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * sizeof(float)));
        }

        return vector;
    }
}
