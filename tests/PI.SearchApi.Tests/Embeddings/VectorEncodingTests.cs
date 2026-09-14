using PI.SearchApi.Embeddings;
using Xunit;

namespace PI.SearchApi.Tests.Embeddings;

public sealed class VectorEncodingTests
{
    [Fact]
    public void RoundTrip_PreservesEveryFloatExactly()
    {
        float[] vector = [0f, 1f, -1f, 0.123456789f, float.Epsilon, -3.5e-7f, 42.25f];

        var decoded = VectorEncoding.FromBase64(VectorEncoding.ToBase64(vector));

        Assert.Equal(vector, decoded);
    }

    [Fact]
    public void ToBase64_OneFloat_IsLittleEndianFloat32()
    {
        // 1.0f is 0x3F800000; little-endian bytes are 00 00 80 3F → base64 "AACAPw==".
        Assert.Equal("AACAPw==", VectorEncoding.ToBase64([1f]));
    }

    [Fact]
    public void FromBase64_ByteCountNotDivisibleByFour_Throws()
    {
        Assert.Throws<FormatException>(() => VectorEncoding.FromBase64(Convert.ToBase64String([1, 2, 3])));
    }
}
