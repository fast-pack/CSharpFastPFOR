using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.LongCompression.Differential;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression;

public class LongDeltaTest
{
    [Fact]
    public void testEmptyArrayFastInverseDelta()
    {
        LongCompressor compressor = new LongCompressor();
        long[] input = [];

        LongDelta.delta(input);
        long[] compressed = compressor.compress(input);
        long[] result = compressor.uncompress(compressed);
        LongDelta.fastinverseDelta(result);

        Assert.NotNull(result);
        Assert2.assertArrayEquals(input, result);
    }
}