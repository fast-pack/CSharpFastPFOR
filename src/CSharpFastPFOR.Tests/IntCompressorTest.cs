/*
 * Testing IntCompressor objects.
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests;

public class IntCompressorTest(ITestOutputHelper _testOutputHelper)
{
    private readonly IntCompressor[] ic =
    [
        new IntCompressor(new VariableByte()),
        new IntCompressor(new SkippableComposition(new BinaryPacking(), new VariableByte()))
    ];

    private readonly IntegratedIntCompressor[] iic =
    [
        new IntegratedIntCompressor(new IntegratedVariableByte()),
        new IntegratedIntCompressor(new SkippableIntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()))
    ];

    [Fact]
    public void basicTest()
    {
        for (int N = 1; N <= 10000; N *= 10)
        {
            int[] orig = new int[N];
            for (int k = 0; k < N; k++)
                orig[k] = (3 * k) + 5;
            foreach (IntCompressor i in ic)
            {
                int[] comp = i.compress(orig);
                int[] back = i.uncompress(comp);
                Assert2.assertArrayEquals(back, orig);
            }
        }
    }

    [Fact]
    public void superSimpleExample()
    {
        IntegratedIntCompressor iic2 = new IntegratedIntCompressor();
        int[] data = new int[2342351];
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;
        _testOutputHelper.WriteLine("Compressing " + data.Length + " integers using friendly interface");
        int[] compressed = iic2.compress(data);
        int[] recov = iic2.uncompress(compressed);
        _testOutputHelper.WriteLine("compressed from " + ((data.Length * 4) / 1024) + "KB to " + ((compressed.Length * 4) / 1024) + "KB");
        if (!Arrays.equals(recov, data)) throw new InvalidOperationException("bug");
    }

    [Fact]
    public void basicIntegratedTest()
    {
        for (int N = 1; N <= 10000; N *= 10)
        {
            int[] orig = new int[N];
            for (int k = 0; k < N; k++)
                orig[k] = (3 * k) + 5;
            foreach (IntegratedIntCompressor i in iic)
            {
                int[] comp = i.compress(orig);
                int[] back = i.uncompress(comp);
                Assert2.assertArrayEquals(back, orig);
            }
        }
    }
}