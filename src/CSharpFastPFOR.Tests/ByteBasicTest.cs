/*
 * Just some basic sanity tests.
 *
 * @author Daniel Lemire
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.Tests.Port;
using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class ByteBasicTest(ITestOutputHelper testOutputHelper)
{
    private readonly ByteIntegerCODEC[] codecs =
    [
        new VariableByte(),
        new IntegratedVariableByte()
    ];

    [Fact]
    public void saulTest()
    {
        foreach (ByteIntegerCODEC C in codecs)
        {
            for (int x = 0; x < 50 * 4; ++x)
            {
                int[] a = [2, 3, 4, 5];
                sbyte[] b = new sbyte[90 * 4];
                int[] c = new int[a.Length];

                IntWrapper aOffset = new IntWrapper(0);
                IntWrapper bOffset = new IntWrapper(x);
                C.compress(a, aOffset, a.Length, b, bOffset);
                int len = bOffset.get() - x;

                bOffset.set(x);
                IntWrapper cOffset = new IntWrapper(0);
                C.uncompress(b, bOffset, len, c, cOffset);
                if (!Arrays.equals(a, c))
                    testOutputHelper.WriteLine("Problem with " + C);
                Assert2.assertArrayEquals(a, c);
            }
        }
    }

    [Fact]
    public void varyingLengthTest()
    {
        const int N = 4096;
        int[] data = new int[N];
        for (int k = 0; k < N; ++k)
            data[k] = k;
        foreach (ByteIntegerCODEC c in codecs)
        {
            for (int L = 1; L <= 128; L++)
            {
                sbyte[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug " + c + " " + k + " " + answer[k] + " " + data[k]);
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                sbyte[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
                }
            }
        }
    }

    [Fact]
    public void varyingLengthTest2()
    {
        const int N = 128;
        int[] data = new int[N];
        data[127] = -1;
        foreach (ByteIntegerCODEC c in codecs)
        {
            //TODO: this makes no sense in port
            //if (c is Simple9)
            //    continue;

            for (int L = 1; L <= 128; L++)
            {
                sbyte[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug at k = " + k + " " + answer[k] + " " + data[k]);
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                sbyte[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
                }
            }
        }
    }
}