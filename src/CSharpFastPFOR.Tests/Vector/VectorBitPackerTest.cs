using Genbox.CSharpFastPFOR.IntegerCompression.Vector;
using static Genbox.CSharpFastPFOR.IntegerCompression.Vector.VectorBitPackerKernels;

namespace Genbox.CSharpFastPFOR.Tests.Vector;

public class VectorBitPackerTest
{
    [Fact]
    public void roundTripAcrossLaneWidths()
    {
        roundTrip(LaneWidth.BITS_128);
        roundTrip(LaneWidth.BITS_256);
        roundTrip(LaneWidth.BITS_512);
    }

    private static void roundTrip(LaneWidth laneWidth)
    {
        Random random = new Random(42);
        VectorBitPackerKernels packer = laneWidth.Kernel;
        for (int b = 1; b <= 32; b++)
        {
            int mask = b == 32 ? -1 : (1 << b) - 1;
            int[] input = new int[VectorFastPFOR.BLOCK_SIZE];
            for (int i = 0; i < input.Length; i++)
                input[i] = random.Next() & mask;

            int[] packed = new int[8 * b];
            packer.fastpack(input, 0, packed, 0, b);
            int[] recovered = new int[input.Length];
            packer.fastunpack(packed, 0, recovered, 0, b);
            Assert.Equal(input, recovered);

            int[] packedNoMask = new int[8 * b];
            packer.fastpackNoMask(input, 0, packedNoMask, 0, b);
            int[] recoveredNoMask = new int[input.Length];
            packer.fastunpack(packedNoMask, 0, recoveredNoMask, 0, b);
            Assert.Equal(input, recoveredNoMask);
            Assert.Equal(packed, packedNoMask);
        }
    }
}