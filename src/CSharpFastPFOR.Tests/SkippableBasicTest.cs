/**
 * Just some basic sanity tests.
 * 
 * @author Daniel Lemire
 */

using Genbox.CSharpFastPFOR.Port;
using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class SkippableBasicTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SkippableBasicTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private readonly SkippableIntegerCODEC[] codecs =
    [
        new JustCopy(),
        new VariableByte(),
        new SkippableComposition(new BinaryPacking(), new VariableByte()),
        new SkippableComposition(new NewPFD(), new VariableByte()),
        new SkippableComposition(new NewPFDS9(), new VariableByte()),
        new SkippableComposition(new NewPFDS16(), new VariableByte()),
        new SkippableComposition(new OptPFD(), new VariableByte()),
        new SkippableComposition(new OptPFDS9(), new VariableByte()),
        new SkippableComposition(new OptPFDS16(), new VariableByte()),
        new SkippableComposition(new FastPFOR128(), new VariableByte()),
        new SkippableComposition(new FastPFOR(), new VariableByte()),
        new Simple9(),
        new Simple16()
    ];

    [Fact]
    public void consistentTest()
    {
        const int N = 4096;
        int[] data = new int[N];
        int[] rev = new int[N];
        for (int k = 0; k < N; ++k)
            data[k] = k % 128;
        foreach (SkippableIntegerCODEC c in codecs)
        {
            _testOutputHelper.WriteLine("[SkippeableBasicTest.consistentTest] codec = " + c);
            int[] outBuf = new int[N + 1024];
            for (int n = 0; n <= N; ++n)
            {
                IntWrapper inPos = new IntWrapper();
                IntWrapper outPos = new IntWrapper();
                c.headlessCompress(data, inPos, n, outBuf, outPos);

                IntWrapper inPoso = new IntWrapper();
                IntWrapper outPoso = new IntWrapper();
                c.headlessUncompress(outBuf, inPoso, outPos.get(), rev,
                    outPoso, n);
                if (outPoso.get() != n)
                {
                    throw new Exception("bug " + n);
                }
                if (inPoso.get() != outPos.get())
                {
                    throw new Exception("bug " + n + " " + inPoso.get() + " " + outPos.get());
                }
                for (int j = 0; j < n; ++j)
                    if (data[j] != rev[j])
                    {
                        throw new Exception("bug");
                    }
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
        foreach (SkippableIntegerCODEC c in codecs)
        {
            _testOutputHelper.WriteLine("[SkippeableBasicTest.varyingLengthTest] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug " + c + " " + k + " " + answer[k] + " " + data[k]);
            }
            for (int L = 128; L <= N; L *= 2)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug");
            }

        }
    }

    [Fact]
    public void varyingLengthTest2()
    {
        const int N = 128;
        int[] data = new int[N];
        data[127] = -1;
        foreach (SkippableIntegerCODEC c in codecs)
        {
            _testOutputHelper.WriteLine("[SkippeableBasicTest.varyingLengthTest2] codec = " + c);

            if (c is Simple9)
                continue;

            if (c is Simple16)
                continue;

            for (int L = 1; L <= 128; L++)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug at k = " + k + " " + answer[k] + " " + data[k] + " for " + c);
            }
            for (int L = 128; L <= N; L *= 2)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug");
            }
        }
    }
}