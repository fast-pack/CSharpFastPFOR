/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/*
 * @author lemire
 *
 */

using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.Tests.Port;
using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class XorBinaryPackingTest
{
    private static void checkCompressAndUncompress(int[] data)
    {
        XorBinaryPacking codec = new XorBinaryPacking();
        int[] compBuf = TestUtils.compress(codec, data);
        int[] decompBuf = TestUtils.uncompress(codec, compBuf, data.Length);
        Assert2.assertArrayEquals(data, decompBuf);
    }

    [Fact]
    public void compressAndUncompress0()
    {
        int[] data = new int[128];
        Arrays.fill(data, 0, 31, 1);
        Arrays.fill(data, 32, 63, 2);
        Arrays.fill(data, 64, 95, 4);
        Arrays.fill(data, 96, 127, 8);
        checkCompressAndUncompress(data);
    }

    [Fact]
    public void compressAndUncompress1()
    {
        int[] data = new int[128];
        for (int i = 0; i < data.Length; ++i)
            data[i] = i;
        checkCompressAndUncompress(data);
    }

    [Fact]
    public void compressAndUncompress2()
    {
        int[] data = new int[128];
        for (int i = 0; i < data.Length; ++i)
            data[i] = (i * (i + 1)) / 2;
        checkCompressAndUncompress(data);
    }

    [Fact]
    public void compressAndUncompress3()
    {
        int[] data = new int[256];
        Arrays.fill(data, 0, 127, 2);
        Arrays.fill(data, 128, 255, 3);
        checkCompressAndUncompress(data);
    }

    [Fact]
    public void compressAndUncompress4()
    {
        int[] data = new int[256];
        Arrays.fill(data, 0, 127, 3);
        Arrays.fill(data, 128, 255, 2);
        checkCompressAndUncompress(data);
    }

    [Fact]
    public void compressAndUncompress5()
    {
        int[] data = new int[256];
        for (int i = 0; i < data.Length; ++i)
            data[i] = i;
        checkCompressAndUncompress(data);
    }
}