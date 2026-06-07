using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Vector;
using static Genbox.CSharpFastPFOR.IntegerCompression.Vector.VectorBitPackerKernels;

namespace Genbox.CSharpFastPFOR.Tests.Vector;

public class VectorFastPFORTest
{
    [Fact]
    public void dirtyOutputBufferRoundTrip()
    {
        int[] data = new int[VectorFastPFOR.BLOCK_SIZE];
        for (int i = 0; i < data.Length; i++)
            data[i] = i % 8;
        data[5] = 1 << 20;
        data[200] = 1 << 20;

        VectorFastPFOR codec = new VectorFastPFOR(VectorFastPFOR.DEFAULT_PAGE_SIZE, LaneWidth.BITS_128);
        int[] compressed = new int[2 * data.Length];
        Array.Fill(compressed, -1);
        IntWrapper outpos = new IntWrapper();
        codec.headlessCompress(data, new IntWrapper(), data.Length, compressed, outpos);

        int[] recovered = new int[data.Length];
        codec.headlessUncompress(compressed, new IntWrapper(), outpos.get(), recovered, new IntWrapper(), data.Length);

        Assert.Equal(data, recovered);
    }

    [Fact]
    public void checkDecodableRejectsWiderStream()
    {
        Assert.Throws<InvalidOperationException>(() => VectorFastPFOR.checkDecodable(LaneWidth.BITS_256, LaneWidth.BITS_128));
        Assert.Throws<InvalidOperationException>(() => VectorFastPFOR.checkDecodable(LaneWidth.BITS_512, LaneWidth.BITS_256));
        VectorFastPFOR.checkDecodable(LaneWidth.BITS_128, LaneWidth.BITS_128);
        VectorFastPFOR.checkDecodable(LaneWidth.BITS_128, LaneWidth.BITS_512);
        VectorFastPFOR.checkDecodable(LaneWidth.BITS_256, LaneWidth.BITS_512);
    }

    [Fact]
    public void lowestCommonWidthRoundTripsOnAnyHost()
    {
        int[] data = new int[3 * VectorFastPFOR.BLOCK_SIZE];
        for (int i = 0; i < data.Length; i++)
            data[i] = i % 8;
        data[5] = 1 << 20;
        data[600] = 1 << 25;

        VectorFastPFOR codec = new VectorFastPFOR(VectorFastPFOR.DEFAULT_PAGE_SIZE, LaneWidth.BITS_128);
        int[] compressed = new int[2 * data.Length];
        IntWrapper outpos = new IntWrapper();
        codec.headlessCompress(data, new IntWrapper(), data.Length, compressed, outpos);

        int[] recovered = new int[data.Length];
        codec.headlessUncompress(compressed, new IntWrapper(), outpos.get(), recovered, new IntWrapper(), data.Length);

        Assert.Equal(data, recovered);
    }
}