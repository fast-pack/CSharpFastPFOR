using Genbox.CSharpFastPFOR.Differential;
using Genbox.CSharpFastPFOR.Port;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests;

public class BoundaryTest(ITestOutputHelper testOutputHelper)
{
    private void compressAndUncompress(int length, IntegerCODEC c)
    {
        // Initialize array.
        int[] source = new int[length];
        for (int i = 0; i < source.Length; ++i)
        {
            source[i] = i;
        }

        // Compress an array.
        int[] compressed = new int[length];
        IntWrapper c_inpos = new IntWrapper(0);
        IntWrapper c_outpos = new IntWrapper(0);
        c.compress(source, c_inpos, source.Length, compressed, c_outpos);
        Assert2.assertTrue(c_outpos.get() <= length);

        // Uncompress an array.
        int[] uncompressed = new int[length];
        IntWrapper u_inpos = new IntWrapper(0);
        IntWrapper u_outpos = new IntWrapper(0);
        c.uncompress(compressed, u_inpos, c_outpos.get(), uncompressed,
            u_outpos);

        // Compare between uncompressed and original arrays.
        int[] target = Arrays.copyOf(uncompressed, u_outpos.get());
        if (!Arrays.equals(source, target))
        {
            testOutputHelper.WriteLine("problem with length = " + length + " and " + c);
            testOutputHelper.WriteLine(Arrays.toString(source));
            testOutputHelper.WriteLine(Arrays.toString(target));
        }
        Assert2.assertArrayEquals(source, target);
    }

    private void around32(IntegerCODEC c)
    {
        compressAndUncompress(31, c);
        compressAndUncompress(32, c);
        compressAndUncompress(33, c);
    }

    private void around128(IntegerCODEC c)
    {
        compressAndUncompress(127, c);
        compressAndUncompress(128, c);
        compressAndUncompress(129, c);
    }

    private void around256(IntegerCODEC c)
    {
        compressAndUncompress(255, c);
        compressAndUncompress(256, c);
        compressAndUncompress(257, c);
    }

    private void testBoundary(IntegerCODEC c)
    {
        around32(c);
        around128(c);
        around256(c);
    }

    [Fact]
    public void testIntegratedComposition()
    {
        IntegratedComposition c = new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte());
        testBoundary(c);
    }

    [Fact]
    public void testComposition()
    {
        Composition c = new Composition(new BinaryPacking(), new VariableByte());
        testBoundary(c);
    }
}