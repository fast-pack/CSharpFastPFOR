/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.LongCompression;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression;

/*
 * Edge-cases having caused issue specifically with LongVariableByte.
 *
 * @author Benoit Lacelle
 */
public class TestLongVariableByte : ATestLongCODEC
{
    private readonly LongVariableByte codec = new LongVariableByte();

    protected override LongCODEC getCodec() => codec;

    [Fact]
    public void testCodec_allBitWidths()
    {
        for (int bitWidth = 0; bitWidth <= 64; bitWidth++)
        {
            long value = bitWidth == 0 ? 0 : 1L << (bitWidth - 1);

            int expectedSizeInBytes = Math.Max(1, (bitWidth + 6) / 7);
            int expectedSizeInLongs = expectedSizeInBytes > 8 ? 2 : 1;

            Assert.Equal(expectedSizeInLongs, LongTestUtils.compress((LongCODEC)codec, [value]).Length);
            Assert.Equal(expectedSizeInBytes, LongTestUtils.compress((ByteLongCODEC)codec, [value]).Length);
            Assert.Equal(expectedSizeInLongs,
                LongTestUtils.compressHeadless(codec, [value]).Length);
        }
    }
}