/**
 * Collection of adhoc tests.
 */

using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class AdhocTest
{
    [Fact]
    public void biggerCompressedArray0()
    {
        // No problem: for comparison.
        IntegerCODEC c = new Composition(new FastPFOR128(), new VariableByte());
        TestUtils.assertSymmetry(c, 0, 16384);
        c = new Composition(new FastPFOR(), new VariableByte());
        TestUtils.assertSymmetry(c, 0, 16384);
    }

    [Fact]
    public void biggerCompressedArray1()
    {
        // Compressed array is bigger than original, because of VariableByte.
        IntegerCODEC c = new VariableByte();
        TestUtils.assertSymmetry(c, -1);
    }

    [Fact]
    public void biggerCompressedArray2()
    {
        // Compressed array is bigger than original, because of Composition.
        IntegerCODEC c = new Composition(new FastPFOR128(), new VariableByte());
        TestUtils.assertSymmetry(c, 65535, 65535);
        c = new Composition(new FastPFOR(), new VariableByte());
        TestUtils.assertSymmetry(c, 65535, 65535);
    }
}