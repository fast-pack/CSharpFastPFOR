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
public class TestLongAs2IntsCodec : ATestLongCODEC
{
    private readonly LongAs2IntsCodec codec = new LongAs2IntsCodec();

    protected override LongCODEC getCodec() => codec;

    [Fact]
    public void testCodec_intermediateHighPowerOfTwo()
    {
        Assert.Equal(3, LongTestUtils.compress(codec, [1L << 42]).Length);
    }
}