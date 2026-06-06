using Genbox.CSharpFastPFOR.Port;

namespace Genbox.CSharpFastPFOR.Tests;

public class UtilsTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UtilsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void testPacking()
    {
        int[] outputarray = new int[32];
        for (int b = 1; b < 32; ++b)
        {
            int[] data = new int[32];
            int[] newdata = new int[32];
            int mask = (1 << b) - 1;
            for (int j = 0; j < data.Length; ++j)
            {
                data[j] = mask - (j % mask);
            }
            for (int n = 0; n <= 32; ++n)
            {
                Arrays.fill(outputarray, 0);
                int howmany = Util.pack(outputarray, 0, data, 0, n, b);
                if (howmany != Util.packsize(n, b)) throw new Exception("bug " + n + " " + b);
                Util.unpack(Arrays.copyOf(outputarray, howmany), 0, newdata, 0, n, b);
                for (int i = 0; i < n; ++i)
                    if (newdata[i] != data[i])
                    {
                        _testOutputHelper.WriteLine(Arrays.toString(Arrays.copyOf(data, n)));
                        _testOutputHelper.WriteLine(Arrays.toString(Arrays.copyOf(newdata, n)));
                        throw new Exception("bug " + b + " " + n);
                    }
            }
        }
    }

    [Fact]
    public void testPackingw()
    {
        int[] outputarray = new int[32];
        for (int b = 1; b < 32; ++b)
        {
            int[] data = new int[32];
            int[] newdata = new int[32];
            int mask = (1 << b) - 1;
            for (int j = 0; j < data.Length; ++j)
            {
                data[j] = mask - (j % mask);
            }
            for (int n = 0; n <= 32; ++n)
            {
                Arrays.fill(outputarray, 0);
                int howmany = Util.packw(outputarray, 0, data, n, b);
                if (howmany != Util.packsizew(n, b)) throw new Exception("bug " + n + " " + b);
                Util.unpackw(Arrays.copyOf(outputarray, howmany), 0, newdata, n, b);
                for (int i = 0; i < n; ++i)
                    if (newdata[i] != data[i])
                    {
                        _testOutputHelper.WriteLine(Arrays.toString(Arrays.copyOf(data, n)));
                        _testOutputHelper.WriteLine(Arrays.toString(Arrays.copyOf(newdata, n)));
                        throw new Exception("bug " + b + " " + n);
                    }
            }
        }
    }
}